using BlazorUploader;

namespace BlazorUploaderDemoWasm.Client
{
	public class ChunkSender
	{
		//This is set in NavMenu.razor
		static public Uri? BaseUri { get; set; }

		static public async Task<string> SendChunkAsync(UploaderFile file, byte[] chunkdata, int offset, int datalen)
		{
			if (BaseUri == null)
				throw new Exception("BaseUri is not initialized");
			if (datalen == 0)
				throw new ArgumentOutOfRangeException("datalen");

			var mfdc = new MultipartFormDataContent();

			ByteArrayContent fileContent = new ByteArrayContent(chunkdata, offset, datalen);
			fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.MimeType);
			mfdc.Add(fileContent, "filechunk",file.FileName);
			mfdc.Add(new StringContent("hello world"), "test1");

			using HttpClient client = new HttpClient();
			using HttpRequestMessage req = new HttpRequestMessage();
			req.Method = HttpMethod.Post;
			req.RequestUri = new Uri(BaseUri, "/UploadDemo/ProcessChunk?clientid=" + file.UserTempFilePath + "&offset=" + file.UserTotalReadSize + "&filesize=" + file.FileSize + "&filename=" + Uri.EscapeDataString(file.FileName) + "&mimetype=" + Uri.EscapeDataString(file.MimeType));
			req.Content = mfdc;
			using var response = await client.SendAsync(req);
			if (response.StatusCode != System.Net.HttpStatusCode.OK)
				throw new Exception("ChunkSender upload failed." + response.StatusCode);

			//return the server side response as server side path
			return await response.Content.ReadAsStringAsync();
		}
		static public async Task DeleteFileAsync(UploaderFile file)
		{
			if (BaseUri == null)
				throw new Exception("BaseUri is not initialized");

			using HttpClient client = new HttpClient();
			using HttpRequestMessage req = new HttpRequestMessage();
			req.Method = HttpMethod.Post;
			req.RequestUri = new Uri(BaseUri, "/UploadDemo/ProcessDelete?clientid=" + file.UserTempFilePath + "&offset=" + file.UserTotalReadSize + "&filesize=" + file.FileSize + "&filename=" + Uri.EscapeDataString(file.FileName));
			req.Content = new StringContent("hello");
			using var response = await client.SendAsync(req);
		}
	}
}
