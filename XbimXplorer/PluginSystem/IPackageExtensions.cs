using System.IO;
using NuGet;
using ICSharpCode.SharpZipLib.Zip;
using System;


namespace XbimXplorer.PluginSystem
{
    internal static class PackageExtensions
    {
		/// <summary>
		/// Extract the manifest file to the specified file name
		/// </summary>
		/// <param name="package"></param>
		/// <param name="targetFileName"></param>
		/// <returns>true if successful, false if fail.</returns>
		internal static bool ExtractManifestFile(this IPackage package, string targetFileName)
		{
			using (var fs = package.GetStream())
			using (var zipFile = new ZipFile(fs))
			{
				foreach (ZipEntry entry in zipFile)
				{
					// Skip directories (only process files)
					if (!entry.IsFile)
						continue;

					// Check for .nuspec extension (case-insensitive)
					string extension = Path.GetExtension(entry.Name);
					if (extension == null || !extension.Equals(".nuspec", StringComparison.OrdinalIgnoreCase))
						continue;

					// Ensure the target directory exists
					string targetDirectory = Path.GetDirectoryName(targetFileName);
					if (!Directory.Exists(targetDirectory))
					{
						Directory.CreateDirectory(targetDirectory);
					}

					// Build the output file path using the target file's name
					string outputFilePath = Path.Combine(targetDirectory, Path.GetFileName(targetFileName));

					// Extract the entry by copying its stream to the output file
					using (var entryStream = zipFile.GetInputStream(entry))
					using (var outputStream = File.Create(outputFilePath))
					{
						entryStream.CopyTo(outputStream);
					}

					return true;
				}
			}

			return false;
		}
	}
}
