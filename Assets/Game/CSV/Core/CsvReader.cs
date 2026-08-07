using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// 轻量 CSV 解析器（纯 Runtime，零第三方依赖）
/// 职责：字节流 → 二维表格（含表头行）
///
/// 编码兼容（中文项目关键）：
///   1. UTF-8 带 BOM → 直接解码
///   2. 无 BOM 的 UTF-8 → 严格解码，成功则用
///   3. 严格解码失败 → 回退 GBK（代码页 936，Excel 简体中文「CSV(逗号分隔)」默认保存格式）
///
/// 字段规则：
///   - 分隔符为英文逗号
///   - 字段内含逗号/换行/引号时用 "..." 包裹，内部 " 转义为 ""
///   - 引用态内可含换行
///   - 全空行跳过；以 # 开头的行视为注释跳过（方便策划写说明）
/// </summary>
public static class CsvReader
{
    /// <summary>读取 CSV 文件，返回二维表格（第一行为表头）</summary>
    public static List<string[]> ReadFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("CSV 文件不存在: " + path);

        if (CsvWriterHelper.IsFileLocked(path))
            throw new IOException($"CSV 文件被占用，无法读取：{path}\n请先关闭正在打开该文件的 Excel / 文本编辑器后重试。");

        byte[] bytes = File.ReadAllBytes(path);
        string text = Decode(bytes);
        return Parse(text);
    }

    /// <summary>按 BOM 优先 / 严格 UTF-8 / GBK 回退的顺序解码字节数组</summary>
    public static string Decode(byte[] bytes)
    {
        // 1. UTF-8 BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        // 2. 无 BOM 的 UTF-8：严格解码，非法字节序列抛 DecoderFallbackException
        try
        {
            var strictUtf8 = new UTF8Encoding(false, true);
            return strictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            // 3. 回退 GBK（Excel 简体中文 ANSI 默认编码）
            return Encoding.GetEncoding(936).GetString(bytes);
        }
    }

    /// <summary>逐字符状态机解析 CSV 文本，返回二维表格（含表头行）</summary>
    public static List<string[]> Parse(string text)
    {
        var rows = new List<string[]>();
        var fields = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;

        int len = text.Length;
        for (int i = 0; i < len; i++)
        {
            char c = text[i];

            // 引用态：只处理引号结束与 "" 转义，其余原样追加（含逗号/换行）
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < len && text[i + 1] == '"')  // "" → 字面引号
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ',':
                    fields.Add(field.ToString());
                    field.Clear();
                    break;
                case '\n':
                case '\r':
                    // 行结束（\r\n 只记一次）
                    if (c == '\r' && i + 1 < len && text[i + 1] == '\n')
                        i++;
                    fields.Add(field.ToString());
                    field.Clear();
                    rows.Add(fields.ToArray());
                    fields.Clear();
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        // 末尾无换行的最后一行
        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            rows.Add(fields.ToArray());
        }

        // 过滤全空行与 # 注释行
        var result = new List<string[]>(rows.Count);
        foreach (var row in rows)
        {
            if (IsEmptyRow(row)) continue;
            if (row.Length > 0 && row[0].TrimStart().StartsWith("#")) continue;
            result.Add(row);
        }
        return result;
    }

    private static bool IsEmptyRow(string[] row)
    {
        foreach (var f in row)
        {
            if (!string.IsNullOrEmpty(f)) return false;
        }
        return true;
    }
}
