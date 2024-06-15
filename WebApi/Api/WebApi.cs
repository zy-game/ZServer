using Aspose.Html;
using Markdig;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ZGame;

namespace ZGame.WebApi;

class DocData
{
    public string title;
    public string time;
    public string desc;
}
class DocManager : Service
{
    private const string CONFIG_PATH = "C:/doc/files.ini";
    private List<DocData> docList = new();
    public void Awake()
    {
        docList = new List<DocData>();
        if (File.Exists(CONFIG_PATH))
        {
            docList = JsonConvert.DeserializeObject<List<DocData>>(File.ReadAllText(CONFIG_PATH));
        }
    }

    private void Save()
    {
        File.WriteAllText(CONFIG_PATH, JsonConvert.SerializeObject(docList));
    }

    public void Release()
    {
        docList = new List<DocData>();
    }

    public DocData GetDocData(string name)
    {
        return docList.Find(x => x.title == name);
    }

    public DocData[] GetDocList()
    {
        return docList.ToArray();
    }

    public DocData Add(string name, string desc, string content)
    {
        DocData doc = new DocData();
        doc.title = name;
        doc.desc = desc;
        doc.time = DateTime.Now.ToString("g");
        docList.Add(doc);
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(CONFIG_PATH), name), content);
        Save();
        return doc;
    }

    public void Remove(string name)
    {
        DocData doc = GetDocData(name);
        if (doc is null)
        {
            return;
        }

        docList.Remove(doc);
        Save();
    }
}

class HTMLBuild : IReference
{
    private StringBuilder builder = new();
    public void Release()
    {
        builder.Clear();
    }

    public override string ToString()
    {
        return builder.ToString();
    }


    public void AddChild(string text)
    {
        builder.AppendLine(text);
    }
}

[Route("api")]
[ApiController]
[DisableCors]
public class WebApi : ControllerBase
{
    [HttpGet]
    public async void Wecom()
    {
        this.Response.ContentType = "text/html";
        HTMLDocument document = new HTMLDocument(@"Z:\ZServer\WebApi\Api\web.html");

        var docList = AppCore.GetService<DocManager>().GetDocList();
        Console.WriteLine("filesList:" + docList.Length);
        for (int i = 0; i < docList.Length; i++)
        {
            var str = "";
            str += @"<div class=""layui-timeline-item"">";
            str += @"<i class=""layui-icon layui-timeline-axis""></i>";
            str += @"<div class=""layui-timeline-content layui-text"">";
            str += @"<a class=""layui-timeline-title"" href=" + docList[i].title + "><h3>" + docList[i].title + "</h3></a>";
            str += @"<p>" + docList[i].desc + "</p>";
            str += @"</div>";
            str += @"</div>";
            document.GetElementById("list").InnerHTML += str;
        }

        var data = Encoding.UTF8.GetBytes(document.DocumentElement.OuterHTML);
        await this.Response.Body.WriteAsync(data, 0, data.Length);
    }

    [HttpPost("new")]
    public void NewDoc(string fileName, string desc, [FromBody] string content)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(desc) || string.IsNullOrEmpty(content))
        {
            this.BadRequest();
            return;
        }

        DocData doc = AppCore.GetService<DocManager>().Add(fileName, desc, content);
        if (doc is null)
        {
            this.BadRequest();
            return;
        }

        this.StatusCode((int)HttpStatusCode.OK);
    }

    [HttpGet("{id}")]
    public async void GetDoc(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        DocData doc = AppCore.GetService<DocManager>().GetDocData(id);
        if (doc is null)
        {
            return;
        }
        this.Response.ContentType = "text/html";
        HTMLDocument document = new HTMLDocument(@"Z:\ZServer\WebApi\Api\web.html");
        document.GetElementById("list").InnerHTML += Markdown.ToHtml(System.IO.File.ReadAllText(@"Z:\shdld_project\README.md"));
        var data = Encoding.UTF8.GetBytes(document.DocumentElement.OuterHTML);
        await this.Response.Body.WriteAsync(data, 0, data.Length);
    }

}