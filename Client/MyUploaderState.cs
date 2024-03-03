namespace BlazorUploaderDemoWasm.Client
{

	public class MyUploaderState
	{
		public List<BlazorUploader.UploaderFile> AllFiles { get; } = new List<BlazorUploader.UploaderFile>();

		public List<BlazorUploader.UploaderFile> ErrFiles { get; } = new List<BlazorUploader.UploaderFile>();

		public bool UploadStarted { get; private set; }

		public string? CurrentActionMsg { get; private set; }

		public event Action? StateHasChanged;

		public void InvokeStateHasChanged()
		{
			if (StateHasChanged != null)
			{
				StateHasChanged();
			}
		}



		public void HandleUploaderEvent(BlazorUploader.UploaderEvent e)
		{
			if (e.FilesError.Length != 0)
			{
				//the files are reject by MaxSizeKB/AcceptMimeTypes/AcceptExtensions/..
				ErrFiles.AddRange(e.FilesError);
				InvokeStateHasChanged();
			}

			if (e.FilesAdded.Length != 0)
			{
				if (e.Uploader.Multiple)
				{
					AllFiles.AddRange(e.FilesAdded);
				}
				else
				{
					foreach (var prevfile in AllFiles)
						prevfile.UserCancelled = true;
					AllFiles.Add(e.FilesAdded.First());
				}

				if (!UploadStarted)
				{
					UploadStarted = true;
					_ = ProcessFilesAsync();
				}

				InvokeStateHasChanged();
			}
		}






		async Task ProcessFilesAsync()
		{
			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

			byte[] buffer = BlazorUploader.CoreUploader.CreateBuffer();

			try
			{
				for (int i = 0; i < AllFiles.Count; i++)
				{
					var f = AllFiles[i];
					if (f.Uploader == null || f.UserCancelled || f.UserError != null || f.UserTotalReadSize == f.FileSize)
						continue;

					CurrentActionMsg = "read " + f.FileName;
					InvokeStateHasChanged();

					using Stream s = f.OpenReadOnlyStream();

					DateTime updatetime = DateTime.Now;

					f.UserTotalReadSize = 0;
					f.UserReadStartTime = DateTime.Now;

					// server side id 
					f.UserTempFilePath = f.Uploader.UniqueId + "-" + f.FileId;

					try
					{
						while (true)
						{
							sw.Restart();

							int rc = await s.ReadAsync(buffer, 0, buffer.Length);

							if (rc == 0)
								break;

							if (f.UserCancelled)
								break;

							f.UserKey = await ChunkSender.SendChunkAsync(f, buffer, 0, rc);

							TimeSpan latency = sw.Elapsed;
							if (f.UserMaxReadLatency < latency) f.UserMaxReadLatency = latency;

							f.UserTotalReadSize += rc;
							f.UserTotalReadTimeSpan += latency;
							f.UserTotalReadTimes++;

							if (DateTime.Now - updatetime > TimeSpan.FromSeconds(0.2))
							{
								CurrentActionMsg = "read " + f.FileName + " " + f.UserTotalReadSize;
								updatetime = DateTime.Now;
								InvokeStateHasChanged();
							}

						}
					}
					catch (Exception fx)
					{
						f.UserError = fx.Message;
						try { _ = ChunkSender.DeleteFileAsync(f); } catch { }
						throw;
					}

					if (f.UserCancelled)
					{
						try { _ = ChunkSender.DeleteFileAsync(f); } catch { }
					}
				}

				CurrentActionMsg = "All Done";
			}
			catch (Exception x)
			{
				CurrentActionMsg = "Error " + x.ToString();
			}
			finally
			{
				UploadStarted = false;
			}

			InvokeStateHasChanged();
		}
	}
}
