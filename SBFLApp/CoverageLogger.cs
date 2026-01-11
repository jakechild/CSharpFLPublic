using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SBFLApp;

/// <summary>
/// Centralized logger for coverage data that keeps a shared <see cref="StreamWriter"/> per coverage file
/// to minimize file open/close overhead.
/// </summary>
public static class CoverageLogger
{
    private static readonly ConcurrentDictionary<string, StreamWriter> Writers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object DisposeLock = new();
    private static bool _disposed;

    static CoverageLogger()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DisposeAll();
    }

    /// <summary>
    /// Writes the specified guid to the coverage file using a shared writer.
    /// </summary>
    /// <param name="coverageFilePath">The coverage file to append to.</param>
    /// <param name="guid">The guid to write.</param>
    public static void Log(string coverageFilePath, string guid)
    {
        if (string.IsNullOrWhiteSpace(coverageFilePath))
        {
            return;
        }

        var writer = Writers.GetOrAdd(coverageFilePath, CreateWriter);

        lock (writer)
        {
            writer.WriteLine(guid);
            writer.Flush();
        }
    }

    private static StreamWriter CreateWriter(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var stream = new FileStream(
            path,
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite);

        return new StreamWriter(stream)
        {
            AutoFlush = true
        };
    }

    [SuppressMessage("Major Code Smell", "S3010:Static fields should not be updated in constructors", Justification = "ProcessExit hook disposes shared writer cache.")]
    private static void DisposeAll()
    {
        lock (DisposeLock)
        {
            if (_disposed)
            {
                return;
            }

            foreach (var writer in Writers.Values)
            {
                writer.Dispose();
            }

            Writers.Clear();
            _disposed = true;
        }
    }
}
