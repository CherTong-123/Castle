param([string]$Root = 'D:\PersonalGameDevelop\Castle')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class KingSpritePacker {
 public static void Pack(string input, string output, string preview, string action) {
  Directory.CreateDirectory(output); Directory.CreateDirectory(preview);
  using(var source = new Bitmap(input))
  using(var atlas = new Bitmap(1280,160,PixelFormat.Format32bppArgb))
  using(var contact = new Bitmap(1280,320,PixelFormat.Format32bppArgb)) {
   Rectangle[] bounds = new Rectangle[8]; int[] anchors = new int[8]; int[] crowns = new int[8];
   for(int i=0;i<8;i++) {
    int x0=(int)Math.Round((i%4)*source.Width/4.0), x1=(int)Math.Round((i%4+1)*source.Width/4.0);
    int y0=(int)Math.Round((i/4)*source.Height/2.0), y1=(int)Math.Round((i/4+1)*source.Height/2.0);
    int l=x1,t=y1,r=x0,b=y0; long sum=0; int n=0,ct=y1;
    for(int y=y0;y<y1;y++) for(int x=x0;x<x1;x++) {
     Color c=source.GetPixel(x,y); if(c.A<128) continue;
     l=Math.Min(l,x); t=Math.Min(t,y); r=Math.Max(r,x); b=Math.Max(b,y);
     if(y<y0+(y1-y0)/4 && c.R>140 && c.G>90 && c.B<c.G*0.65 && c.R>c.G*0.95) {sum+=x;n++;ct=Math.Min(ct,y);}
    }
    if(n==0||r<=l) throw new Exception("Missing character/crown: "+i);
    bounds[i]=Rectangle.FromLTRB(l,t,r+1,b+1); anchors[i]=(int)(sum/n); crowns[i]=ct;
   }
   int contactHeight=Math.Max(bounds[0].Bottom-crowns[0],bounds[4].Bottom-crowns[4]);
   double scale=128.0/contactHeight;
   int[] bob=action=="Walk" ? new[]{0,2,0,-2,0,2,0,-2} : new[]{0,3,-1,-3,0,3,-1,-3};
   using(var ag=Graphics.FromImage(atlas)) using(var cg=Graphics.FromImage(contact)) {
    cg.Clear(Color.FromArgb(32,37,48));
    for(int i=0;i<8;i++) using(var frame=new Bitmap(160,160,PixelFormat.Format32bppArgb)) {
     for(int y=0;y<160;y++) for(int x=0;x<160;x++) {
      int sx=(int)Math.Floor((x-96)/scale+anchors[i]);
      int sy=(int)Math.Floor((y-16-bob[i])/scale+crowns[i]);
      if(!bounds[i].Contains(sx,sy)) continue;
      var c=source.GetPixel(sx,sy);
      if(c.A>=128) frame.SetPixel(x,y,Color.FromArgb(255,c.R,c.G,c.B));
     }
     bool visible=false;
     for(int y=0;y<160;y++) for(int x=0;x<160;x++) if(frame.GetPixel(x,y).A>0) {
      visible=true;
      if(x<2||x>157||y<2||y>157) throw new Exception("Clipped frame: "+action+" "+i);
     }
     if(!visible) throw new Exception("Empty frame");
     frame.Save(Path.Combine(output,action+"_"+(i+1).ToString("00")+".png"),ImageFormat.Png);
     ag.DrawImageUnscaled(frame,i*160,0);
     cg.InterpolationMode=InterpolationMode.NearestNeighbor; cg.PixelOffsetMode=PixelOffsetMode.Half;
     cg.DrawImage(frame,new Rectangle((i%4)*320,(i/4)*160,160,160),0,0,160,160,GraphicsUnit.Pixel);
     using(var gif=new Bitmap(320,320)) using(var gg=Graphics.FromImage(gif)) {
      gg.Clear(Color.FromArgb(32,37,48)); gg.InterpolationMode=InterpolationMode.NearestNeighbor; gg.PixelOffsetMode=PixelOffsetMode.Half;
      gg.DrawImage(frame,new Rectangle(0,0,320,320),0,0,160,160,GraphicsUnit.Pixel);
      gif.Save(Path.Combine(preview,action+"_"+i+".gif"),ImageFormat.Gif);
     }
    }
   }
   atlas.Save(Path.Combine(Path.GetDirectoryName(output),"King_"+action+"_Strip.png"),ImageFormat.Png);
   contact.Save(Path.Combine(preview,action+"_Contact.png"),ImageFormat.Png);
  }
 }
}
'@
$pack = Join-Path $Root 'Assets\Art\KingMovement'
$preview = Join-Path $Root 'ArtExports\KingMovement_v2\Preview'
[KingSpritePacker]::Pack((Join-Path $Root 'ArtExports\KingMovement_v2\Source\Walk.png'),(Join-Path $pack 'Frames\Walk'),$preview,'Walk')
[KingSpritePacker]::Pack((Join-Path $Root 'ArtExports\KingMovement_v2\Source\Run.png'),(Join-Path $pack 'Frames\Run'),$preview,'Run')


