using System.Text;

public static class ReportFiles
{
    public static string Csv(string text)
    {
        if (text.Length > 0 && "=+-@\t\r".Contains(text[0]))
            text = "'" + text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    public static byte[] Pdf(List<string[]> rows)
    {
        // PDF paginado sin librería externa; usa Helvetica/WinAnsi y conserva filas completas.
        var lines = new List<string>
        {
            "Portal de Adopcion - Reporte",
            $"Registros: {Math.Max(0, rows.Count - 1)}"};
        foreach (var row in rows)
        {
            var text = string.Join(" | ", row);
            for (int k = 0; k < text.Length; k += 95)
                lines.Add(text.Substring(k, Math.Min(95, text.Length - k)));
            lines.Add("");
        }

        var chunks = lines.Chunk(48).ToArray();
        var objects = new List<byte[]>();
        var enc = Encoding.Latin1;
        byte[] B(string s) => enc.GetBytes(s);
        objects.Add(B("<< /Type /Catalog /Pages 2 0 R >>"));
        objects.Add([]);
        objects.Add(B("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
        var ids = new List<int>();
        foreach (var page in chunks)
        {
            var pageId = objects.Count + 1;
            var streamId = pageId + 1;
            ids.Add(pageId);
            objects.Add(B($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {streamId} 0 R >>"));
            var content = "BT /F1 9 Tf 44 800 Td 14 TL " + string.Join(" ", page.Select(line => "(" + line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\r", " ").Replace("\n", " ") + ") Tj T*")) + " ET";
            var bytes = B(content);
            objects.Add(B($"<< /Length {bytes.Length} >>\nstream\n").Concat(bytes).Concat(B("\nendstream")).ToArray());
        }

        objects[1] = B($"<< /Type /Pages /Count {ids.Count} /Kids [{string.Join(" ", ids.Select(i => $"{i} 0 R"))}] >>");
        using var ms = new MemoryStream();
        void Write(string s) => ms.Write(B(s));
        Write("%PDF-1.4\n");
        var offsets = new List<long>
        {
            0
        };
        for (int i = 0; i < objects.Count; i++)
        {
            offsets.Add(ms.Position);
            Write($"{i + 1} 0 obj\n");
            ms.Write(objects[i]);
            Write("\nendobj\n");
        }

        var xref = ms.Position;
        Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            Write($"{offset:0000000000} 00000 n \n");
        Write($"trailer << /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return ms.ToArray();
    }
}
