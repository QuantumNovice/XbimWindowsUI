using ICSharpCode.SharpZipLib.Zip;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Xbim.BCF
{
    public class BcfFile 
    {
        public ObservableCollection<BcfInstance> Instances = new ObservableCollection<BcfInstance>();

        private const string MarkupFileName = "markup.bcf";
        private const string ViewpointFileName = "viewpoint.bcfv";
        private const string SnapshotFileName = "snapshot.png";


		public void LoadFile(string fileName)
		{
			// Compile the regex pattern to extract the GUID and file name.
			Regex regex = new Regex(@"(?<guid>.*?)/(?<fname>.*)", RegexOptions.Compiled);

			// Open the ZIP file for reading.
			using (FileStream fs = File.OpenRead(fileName))
			using (ZipFile zipFile = new ZipFile(fs))
			{
				foreach (ZipEntry entry in zipFile)
				{
					// Create a temporary file to extract the entry.
					string tempFilePath = Path.GetTempFileName();

					// Try matching the entry name against the expected pattern.
					var match = regex.Match(entry.Name);
					if (!match.Success)
						continue;

					// Extract the entry to the temporary file.
					using (Stream entryStream = zipFile.GetInputStream(entry))
					using (FileStream tempFileStream = File.OpenWrite(tempFilePath))
					{
						entryStream.CopyTo(tempFileStream);
					}

					// Retrieve the GUID and inner file name from the match groups.
					string guid = match.Groups["guid"].Value;
					string innerFileName = match.Groups["fname"].Value;

					// If no instance exists with this GUID, create a new one.
					if (!Instances.Any(x => x.Markup.Topic.Guid == guid))
					{
						Instances.Add(new BcfInstance(guid));
					}

					// Retrieve the instance for the given GUID.
					BcfInstance instance = Instances.First(x => x.Markup.Topic.Guid == guid);

					// Process the temporary file based on its name (case-insensitive).
					switch (innerFileName.ToLowerInvariant())
					{
						case MarkupFileName:
							instance.Markup = Markup.LoadFromFile(tempFilePath);
							break;
						case ViewpointFileName:
							instance.VisualizationInfo = VisualizationInfo.LoadFromFile(tempFilePath);
							break;
						case SnapshotFileName:
							var bitmap = new BitmapImage();
							bitmap.BeginInit();
							bitmap.CacheOption = BitmapCacheOption.OnLoad;
							bitmap.UriSource = new Uri(tempFilePath);
							bitmap.EndInit();
							instance.SnapShot = bitmap;
							break;
						default:
							// Unknown file type – ignore or add additional handling here.
							break;
					}

					// Delete the temporary file.
					File.Delete(tempFilePath);
				}
			}
		}

		private string GetTemporaryDirectory(string guid)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), guid);
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

		internal void SaveFile(string filename)
		{
			// Create the zip file for writing.
			using (FileStream fs = File.Create(filename))
			using (ZipOutputStream zipStream = new ZipOutputStream(fs))
			{
				// Optionally set the compression level (0-9).
				zipStream.SetLevel(9); // maximum compression

				foreach (var instance in Instances)
				{
					string tempDir = GetTemporaryDirectory(instance.Guid);
					// Save each required file into the temporary directory.
					instance.Markup.SaveToFile(Path.Combine(tempDir, MarkupFileName));
					instance.SnapShotSaveToFile(Path.Combine(tempDir, SnapshotFileName));
					instance.VisualizationInfo.SaveToFile(Path.Combine(tempDir, ViewpointFileName));

					// Add the entire directory into the zip, placing it under a folder named for the instance Guid.
					AddDirectoryToZip(zipStream, tempDir, instance.Guid);
				}
				// Finalize the zip archive.
				zipStream.Finish();
			}

			// Remove temporary directories after zipping.
			foreach (var instance in Instances)
			{
				Directory.Delete(GetTemporaryDirectory(instance.Guid), true);
			}
		}

		/// <summary>
		/// Recursively adds all files from the specified folder into the zip archive.
		/// The files will be placed under a root folder named 'folderEntryName' in the zip.
		/// </summary>
		/// <param name="zipStream">The zip output stream.</param>
		/// <param name="folderPath">The local folder path to add.</param>
		/// <param name="folderEntryName">The folder name to use inside the zip.</param>
		private void AddDirectoryToZip(ZipOutputStream zipStream, string folderPath, string folderEntryName)
		{
			// Get all files (including in subdirectories).
			var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
			foreach (var file in files)
			{
				// Determine the file's relative path to the folder being zipped.
				string relativePath = file.Substring(folderPath.Length + 1);
				// Combine with the folderEntryName to form the full entry name in the zip.
				string entryName = Path.Combine(folderEntryName, relativePath);
				// Zip entry names must use forward slashes.
				entryName = entryName.Replace("\\", "/");

				// Create a new zip entry.
				ZipEntry entry = new ZipEntry(entryName)
				{
					DateTime = File.GetLastWriteTime(file)
				};
				zipStream.PutNextEntry(entry);

				// Copy the file into the zip stream.
				using (FileStream fs = File.OpenRead(file))
				{
					fs.CopyTo(zipStream);
				}
				zipStream.CloseEntry();
			}
		}
	}
}
