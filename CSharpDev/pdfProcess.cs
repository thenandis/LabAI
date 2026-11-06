using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
// using Tesseract;

public class PdfProcessor
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task ProcessPdfAsync(string pdfUrl, CancellationToken cancellationToken)
    {
        try
        {
            // Download the PDF file
            var pdfBytes = await httpClient.GetByteArrayAsync(pdfUrl, cancellationToken);
            Console.WriteLine($"Downloaded PDF from {pdfUrl}, Size: {pdfBytes.Length} bytes");

            // Simulate processing the PDF (e.g., extracting text)
            await Task.Delay(500, cancellationToken); // Simulating processing delay
            Console.WriteLine($"Processed PDF from {pdfUrl}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"Processing of PDF from {pdfUrl} was canceled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing PDF from {pdfUrl}: {ex.Message}");
        }
    }

    public async Task ProcessMultiplePdfsAsync(string[] pdfUrls, CancellationToken cancellationToken)
    {
        var tasks = new ConcurrentBag<Task>();

        foreach (var url in pdfUrls)
        {
            tasks.Add(ProcessPdfAsync(url, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }
}