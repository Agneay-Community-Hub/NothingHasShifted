using System; using System.Collections; using System.Collections.Generic; using System.Globalization; using System.IO; using System.Text;
public static class MiniJSON {
  public static object Deserialize(string json){ return json==null?null:new Parser(json).ParseValue(); }
  sealed class Parser { StringReader r; public Parser(string s){ r=new StringReader(s);} 
    char Peek=>(char)r.Peek(); char Next=>(char)r.Read();
    void Ws(){ while(r.Peek()!=-1 && char.IsWhiteSpace(Peek)) r.Read(); }
    public object ParseValue(){ Ws(); char c=Peek; switch(c){ case '{': return Obj(); case '[': return Arr(); case '"': return Str();
      case 't': for(int i=0;i<4;i++)r.Read(); return true; case 'f': for(int i=0;i<5;i++)r.Read(); return false; case 'n': for(int i=0;i<4;i++)r.Read(); return null; default: return Num(); } }
    Dictionary<string,object> Obj(){ var d=new Dictionary<string,object>(); r.Read(); while(true){ Ws(); if(Peek=='}'){r.Read();return d;} if(Peek==','){r.Read();continue;} string k=Str(); Ws(); r.Read(); d[k]=ParseValue(); } }
    List<object> Arr(){ var l=new List<object>(); r.Read(); while(true){ Ws(); if(Peek==']'){r.Read();return l;} if(Peek==','){r.Read();continue;} l.Add(ParseValue()); } }
    string Str(){ var sb=new StringBuilder(); r.Read(); while(true){ int ci=r.Read(); if(ci==-1) break; char c=(char)ci; if(c=='"') break; if(c=='\\'){ char e=Next; switch(e){ case 'n': sb.Append('\n'); break; case 't': sb.Append('\t'); break; case 'r': break; case 'b': break; case 'f': break; case 'u': var h=new char[4]; r.Read(h,0,4); sb.Append((char)Convert.ToInt32(new string(h),16)); break; default: sb.Append(e); break; } } else sb.Append(c);} return sb.ToString(); }
    object Num(){ var sb=new StringBuilder(); while(r.Peek()!=-1 && "+-0123456789.eE".IndexOf(Peek)>=0) sb.Append(Next); double d; double.TryParse(sb.ToString(),NumberStyles.Float,CultureInfo.InvariantCulture,out d); return d; }
  }
}
