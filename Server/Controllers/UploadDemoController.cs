using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace BlazorUploaderDemoWasm.Server.Controllers
{
	public class UploadDemoController : ControllerBase
	{
		string GetServerFilePath(string clientid)
		{
			//the data send from client is not safe.
			if (string.IsNullOrEmpty(clientid))
				throw new ArgumentNullException();
			if (clientid.Length > 40)
				throw new ArgumentException();
			foreach (char c in clientid)
			{
				if (char.IsLetterOrDigit(c)) continue;
				if (c == '_' || c == '-') continue;
				throw new ArgumentException("invalid char " + c);
			}

			string userid = "user_" + this.HttpContext.Connection.RemoteIpAddress;
			userid = userid.Replace(':', '_');

			string userfolder = Path.Combine(Environment.CurrentDirectory, "UploadData", userid);
			if (!Directory.Exists(userfolder)) Directory.CreateDirectory(userfolder);

			return Path.Combine(userfolder, clientid + ".filedat");
		}

		[Route("/UploadDemo/ProcessChunk")]
		[HttpPost]
		public async Task<object> ProcessChunkAsync(string clientid, long offset, string filename, long filesize, string mimetype, [FromForm(Name = "filechunk")] IFormFile filechunk)
		{
			if (filechunk == null)
				throw new Exception("unable to get filechunk");

			string serverpath = GetServerFilePath(clientid);

			string jsonfile = serverpath + ".json";
			if (!System.IO.File.Exists(jsonfile))
			{
				string jsontext = System.Text.Json.JsonSerializer.Serialize(new JsonFileInfo { filename = filename, filesize = filesize, mimetype = mimetype });
				await System.IO.File.WriteAllTextAsync(jsonfile, jsontext, Encoding.UTF8);
			}

			using (FileStream fs = new FileStream(serverpath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
			{
				if (offset > 0) fs.Seek(offset, SeekOrigin.Begin);

				await filechunk.CopyToAsync(fs);
			}

			//in this demo , return the serverpath as f.UserKey
			return Content(serverpath, "text/plain");
		}

		[Route("/UploadDemo/ProcessDelete")]
		[HttpPost]
		public void ProcessDelete(string clientid)
		{
			string serverpath = GetServerFilePath(clientid);
			if (System.IO.File.Exists(serverpath))
				System.IO.File.Delete(serverpath);
		}

		[Route("/UploadDemo/DownloadFile")]
		[HttpGet]
		public object DownloadFile(string clientid)
		{
			string serverpath = GetServerFilePath(clientid);
			if (!System.IO.File.Exists(serverpath))
				throw new FileNotFoundException();
			string jsonfile = serverpath + ".json";
			if (!System.IO.File.Exists(jsonfile))
				throw new FileNotFoundException();
			string jsontext = System.IO.File.ReadAllText(jsonfile, Encoding.UTF8);
			var info = System.Text.Json.JsonSerializer.Deserialize<JsonFileInfo>(jsontext);

			return File(new FileStream(serverpath, FileMode.Open, FileAccess.Read), info.mimetype, info.filename, true);
		}

		public class JsonFileInfo
		{
			public string filename { get; set; }
			public long filesize { get; set; }
			public string mimetype { get; set; }
		}
	}
}
