using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using TsConverter.Properties;
using Exception = System.Exception;

namespace TsConverter
{
	public class Converter
	{
		private static FileSystemWatcher tsFsw;
		private static Queue<ConversionPackage> cpQueue;
		private static Logger logger;
		private static int retryCounter;

		public void Start()
		{
			logger = LogManager.GetCurrentClassLogger();
			logger.Info($"TsConverter started on {Environment.MachineName}.");
			cpQueue = new Queue<ConversionPackage>();
			tsFsw = new FileSystemWatcher(Settings.Default.RootDirectory, "*.ts") { IncludeSubdirectories = true, EnableRaisingEvents = true };
			tsFsw.Created += TsFsw_Created;
		}

		private static void TsFsw_Created(object sender, FileSystemEventArgs e)
		{
			if (e.FullPath.Contains(".grab")) return;
			FileInfo ifi = null;
			while (ifi == null)
			{
				try
				{
					ifi = new FileInfo(e.FullPath);
				}
				catch (IOException)
				{
					Thread.Sleep(1000);
				}
				catch (Exception ex)
				{
					logger.Error(ex, $"{MethodBase.GetCurrentMethod()}: {ex.Message} {Environment.NewLine} {ex.StackTrace}");
					return;
				}
			}

			if (ifi.Length <= 0)
			{
				try
				{
					File.Delete(ifi.FullName);
					logger.Info($"{MethodBase.GetCurrentMethod()}: 0 length file {ifi.FullName} deleted.");
					return;
				}
				catch (Exception ex)
				{
					logger.Info($"{MethodBase.GetCurrentMethod()}: Error deleting 0 length file {ifi.FullName}.{Environment.NewLine}" +
								$"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
					return;
				}
			}

			try
			{
				var cp = new ConversionPackage { Ifi = ifi };
				cp.OfPath = cp.Ifi.FullName.Replace(".ts", $"{Settings.Default.OutputFileType}");
				cpQueue.Enqueue(cp);
				logger.Info($"Input file {cp.Ifi.FullName} ({BytesToString(cp.Ifi.Length)}) enqueued for processing.");
				if (cpQueue.Count != 1) return;
				StartFileProcessing(cp);
			}
			catch (Exception ex)
			{
				logger.Error(ex, $"{MethodBase.GetCurrentMethod()}: {ex.Message} {Environment.NewLine} {ex.StackTrace}");
			}
		}

		private static void StartFileProcessing(ConversionPackage cp)
		{
			try
			{
				var psi = new ProcessStartInfo
				{
					CreateNoWindow = true,
					UseShellExecute = false,
					FileName = Settings.Default.ExecutableName,
					Arguments = $"-i \"{cp.Ifi.FullName}\" -o \"{cp.OfPath}\" {Settings.Default.ConvertOptions}"
				};

				var hbp = Process.Start(psi);
				if (hbp == null) throw new ApplicationException("Failed to start Handbrake.");
				hbp.EnableRaisingEvents = true;
				hbp.Exited += Hb_Exited;
				logger.Info($"Processing started for input file {cp.Ifi.FullName} ({BytesToString(cp.Ifi.Length)})");
			}
			catch (Exception ex)
			{
				logger.Error(ex, $"{MethodBase.GetCurrentMethod()}: {ex.Message} {Environment.NewLine} {ex.StackTrace}");
			}
		}

		private static void Hb_Exited(object sender, EventArgs e)
		{
			try
			{
				var activeCp = cpQueue.Peek();

				if (!File.Exists(activeCp.OfPath))
				{
					if (retryCounter < 10)
					{
						retryCounter++;
						var hb = sender as Process;
						logger.Warn($"{MethodBase.GetCurrentMethod()}: Output file {activeCp.OfPath} not found.{Environment.NewLine}" +
									$"Process {hb?.StartInfo.FileName} exited with code {hb?.ExitCode}. " +
									$"Retrying file {activeCp.Ifi.FullName}. Retry count: {retryCounter}.");
						StartFileProcessing(activeCp);
						return;
					}
					logger.Error($"{MethodBase.GetCurrentMethod()}: Output file {activeCp.OfPath} not found.");
				}

				retryCounter = 0;
				if (Settings.Default.DeleteInputFile && File.Exists(activeCp.OfPath))
				{
					try
					{
						File.Delete(activeCp.Ifi.FullName);
						logger.Info($"{MethodBase.GetCurrentMethod()}: Input file {activeCp.Ifi.FullName} deleted.");
					}
					catch (Exception ex)
					{
						logger.Error(ex, $"{MethodBase.GetCurrentMethod()}: Error deleting input file {activeCp.Ifi.FullName}.");
					}
				}

				if (File.Exists(activeCp.OfPath))
				{
					var ofi = new FileInfo(activeCp.OfPath);
					logger.Info($"{MethodBase.GetCurrentMethod()}: Input file {activeCp.Ifi.FullName} ({BytesToString(activeCp.Ifi.Length)}){Environment.NewLine}" +
								$"converted to output file {ofi.FullName} ({BytesToString(ofi.Length)}).");
				}
				else
				{
					logger.Error($"{MethodBase.GetCurrentMethod()}: Failed to convert input file {activeCp.Ifi.FullName}.");
				}

				cpQueue.Dequeue();

				if (cpQueue.Count <= 0)
				{
					cpQueue = new Queue<ConversionPackage>();
					logger.Info($"{MethodBase.GetCurrentMethod()}: Processing queue is empty.");
					return;
				}

				logger.Info($"{cpQueue.Count} file(s) remain in the queue.");
				var cpToProcess = cpQueue.Peek();
				StartFileProcessing(cpToProcess);
			}
			catch (Exception ex)
			{
				logger.Error(ex, $"{MethodBase.GetCurrentMethod()}: {ex.Message} {Environment.NewLine} {ex.StackTrace}");
				var qCount = cpQueue.Count;
				cpQueue = new Queue<ConversionPackage>();
				logger.Info($"{qCount} queued files failed to process.");
			}
		}

		private static string BytesToString(long byteCount)
		{
			string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
			if (byteCount == 0)
				return "0" + suf[0];
			var bytes = Math.Abs(byteCount);
			var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
			var num = Math.Round(bytes / Math.Pow(1024, place), 1);
			return (Math.Sign(byteCount) * num) + suf[place];
		}

		public void Stop()
		{
			logger.Info($"TsConverter stopped on {Environment.MachineName}.");
			LogManager.Shutdown();
			tsFsw.Dispose();
		}
	}
}
