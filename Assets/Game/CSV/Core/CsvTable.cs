using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>
/// CSV 表格：表头映射 + 类型化取值 + 写出（UTF-8 BOM）
/// 列顺序可任意调整，按表头名取数；未知列忽略；缺列由调用方校验。
/// 数值统一用 InvariantCulture 解析/格式化，避免 zh-CN 文化差异导致的 "1,5" 类问题。
/// </summary>
public class CsvTable
{
    public string[] Headers;
    public List<string[]> Rows = new List<string[]>();

    private readonly Dictionary<string, int> _headerIndex;

    public CsvTable(string[] headers, List<string[]> rows)
    {
        Headers = headers ?? new string[0];
        Rows = rows ?? new List<string[]>();
        _headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < Headers.Length; i++)
            _headerIndex[Headers[i].Trim()] = i;
    }

    /// <summary>是否存在该列</summary>
    public bool HasColumn(string name) => _headerIndex.ContainsKey(name);

    /// <summary>按列名取值；无此列或列为空返回 null</summary>
    public string Get(string[] row, string col)
    {
        if (!_headerIndex.TryGetValue(col, out int idx)) return null;
        if (idx >= row.Length) return null;
        string v = row[idx]?.Trim();
        return string.IsNullOrEmpty(v) ? null : v;
    }

    public string GetString(string[] row, string col) => Get(row, col) ?? "";

    public int GetInt(string[] row, string col)
    {
        string v = Get(row, col);
        return v == null ? 0 : int.Parse(v, CultureInfo.InvariantCulture);
    }

    public float GetFloat(string[] row, string col)
    {
        string v = Get(row, col);
        return v == null ? 0f : float.Parse(v, CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// CSV 写出辅助（导出工具用）
/// </summary>
public static class CsvWriterHelper
{
    /// <summary>字段转义：含 , " 换行 时用引号包裹，内部 " 转 ""</summary>
    public static string Escape(string field)
    {
        if (field == null) return "";
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        return field;
    }

    /// <summary>
    /// 检测文件是否被其它程序占用（Excel / 文本编辑器 / 索引服务等）。
    /// 文件不存在返回 false（视为可写）。
    /// </summary>
    public static bool IsFileLocked(string path)
    {
        if (!File.Exists(path)) return false;
        try
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            return false;
        }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
    }

    /// <summary>整表写出为 CSV 文件（UTF-8 带 BOM，Excel 直接打开不乱码）</summary>
    public static void WriteFile(string path, string[] headers, List<string[]> rows)
    {
        if (IsFileLocked(path))
            throw new IOException($"CSV 文件被占用，无法写入：{path}\n请先关闭正在打开该文件的 Excel / 文本编辑器后重试。");

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", EscapeAll(headers)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", EscapeAll(row)));

        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        byte[] preamble = Encoding.UTF8.GetPreamble();          // EF BB BF
        byte[] content = Encoding.UTF8.GetBytes(sb.ToString());
        var bytes = new byte[preamble.Length + content.Length];
        Array.Copy(preamble, 0, bytes, 0, preamble.Length);
        Array.Copy(content, 0, bytes, preamble.Length, content.Length);
        File.WriteAllBytes(path, bytes);
    }

    private static string[] EscapeAll(IEnumerable<string> fields)
    {
        var list = new List<string>();
        foreach (var f in fields)
            list.Add(Escape(f));
        return list.ToArray();
    }
}
