using System.Collections.Generic; using System.IO; using UnityEditor; using UnityEngine; using UnityEngine.Networking;
// Downloads CC0 assets from Poly Haven (polyhaven.com, CC0 license) into Assets/ThirdParty/PolyHaven.
public static class PolyHaven {
  const string Root = "Assets/ThirdParty/PolyHaven";
  static byte[] Get(string url){ var r=UnityWebRequest.Get(url); var op=r.SendWebRequest(); var t=System.DateTime.Now; while(!op.isDone && (System.DateTime.Now-t).TotalSeconds<120) System.Threading.Thread.Sleep(20); if(r.result!=UnityWebRequest.Result.Success) throw new System.Exception(url+" : "+r.error); return r.downloadHandler.data; }
  static Dictionary<string,object> D(object o)=> (Dictionary<string,object>)o;
  public static string Model(string id, string res="1k"){
    var files=D(MiniJSON.Deserialize(System.Text.Encoding.UTF8.GetString(Get("https://api.polyhaven.com/files/"+id))));
    var g=D(D(D(files["gltf"])[res])["gltf"]);
    string dir=Path.Combine(Root,"Models",id); Directory.CreateDirectory(dir);
    if(g.ContainsKey("include")) foreach(var kv in D(g["include"])){ var p=Path.Combine(dir,kv.Key); Directory.CreateDirectory(Path.GetDirectoryName(p)); File.WriteAllBytes(p,Get((string)D(kv.Value)["url"])); }
    string url=(string)g["url"]; File.WriteAllBytes(Path.Combine(dir,id+".gltf"),Get(url));
    return dir;
  }
  public static string Texture(string id, string res="1k"){
    var files=D(MiniJSON.Deserialize(System.Text.Encoding.UTF8.GetString(Get("https://api.polyhaven.com/files/"+id))));
    string dir=Path.Combine(Root,"Textures",id); Directory.CreateDirectory(dir);
    foreach(var map in new[]{"Diffuse","nor_gl","Rough","AO","arm"}){ if(!files.ContainsKey(map)) continue; var m=D(files[map]); if(!m.ContainsKey(res)) continue; var f=D(m[res]); string key=f.ContainsKey("jpg")?"jpg":"png"; File.WriteAllBytes(Path.Combine(dir,id+"_"+map+"."+key),Get((string)D(f[key])["url"])); }
    return dir;
  }
  public static string Search(string type, string words){
    var all=D(MiniJSON.Deserialize(System.Text.Encoding.UTF8.GetString(Get("https://api.polyhaven.com/assets?t="+type))));
    var sb=new System.Text.StringBuilder(); var ws=words.Split(' ');
    foreach(var kv in all){ var a=D(kv.Value); string hay=kv.Key+" "+a["name"]; if(a.ContainsKey("tags")) foreach(var t in (List<object>)a["tags"]) hay+=" "+t; if(a.ContainsKey("categories")) foreach(var t in (List<object>)a["categories"]) hay+=" "+t; hay=hay.ToLower(); foreach(var w in ws) if(hay.Contains(w)){ sb.Append(kv.Key).Append(' '); break; } }
    return sb.ToString();
  }
}
