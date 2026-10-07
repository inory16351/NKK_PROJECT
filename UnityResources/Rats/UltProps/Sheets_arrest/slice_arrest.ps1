$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
# Tools/slice_rat_parts.py key_magenta와 같은 거리·알파·탈색 계산.
# 연결 영역 전체를 중심점이 속한 칸에 배정해 칸 경계를 넘은 꼬리도 보존한다.
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.IO;
public static class ArrestSlice {
    class Part {
        public int id, area, cell;
        public int x0=int.MaxValue,y0=int.MaxValue,x1=-1,y1=-1;
        public long sx,sy;
    }
    static int Byte(double n) { return (int)Math.Max(0,Math.Min(255,n)); }
    public static void Run(string input,string output,string preview) {
        string[] names={"police_rat_a","police_rat_b","police_rat_cuff","lawyer_rat_point","lawyer_rat_doc","prop_handcuffs","police_tape","police_siren"};
        using(var source=new Bitmap(input)) {
            int w=source.Width,h=source.Height,n=w*h;
            int[] pixels=new int[n],labels=new int[n],queue=new int[n];
            for(int y=0;y<h;y++) for(int x=0;x<w;x++) {
                Color c=source.GetPixel(x,y);
                double d=Math.Sqrt((c.R-255.0)*(c.R-255.0)+c.G*c.G+(c.B-255.0)*(c.B-255.0));
                double a=Math.Max(0,Math.Min(1,(d-60)/60)),k=1-a;
                if(a<=0.02) continue;
                pixels[y*w+x]=Color.FromArgb(Byte(a*255),Byte((c.R-255*k)/Math.Max(a,0.001)),c.G,Byte((c.B-255*k)/Math.Max(a,0.001))).ToArgb();
            }
            // 키잉 후 가장자리의 자홍 잔색만 가까운 불투명 내부 색으로 복구한다.
            int[] keyed=(int[])pixels.Clone();
            for(int y=2;y<h-2;y++) for(int x=2;x<w-2;x++) {
                int q=y*w+x;Color c=Color.FromArgb(keyed[q]);
                if(c.A==0 || c.R<=c.G+25 || c.B<=c.G+25) continue;
                bool edge=false;
                for(int dy=-2;dy<=2;dy++) for(int dx=-2;dx<=2;dx++)
                    if(((uint)keyed[(y+dy)*w+x+dx]>>24)==0) edge=true;
                if(!edge) continue;
                Color best=c;int distance=999;
                for(int dy=-3;dy<=3;dy++) for(int dx=-3;dx<=3;dx++) {
                    int xx=x+dx,yy=y+dy;if(xx<0||yy<0||xx>=w||yy>=h) continue;
                    Color near=Color.FromArgb(keyed[yy*w+xx]);int d=dx*dx+dy*dy;
                    if(near.A>250 && (near.R<=near.G+20 || near.B<=near.G+20) && d<distance) {best=near;distance=d;}
                }
                if(distance<999) pixels[q]=Color.FromArgb(c.A,best.R,best.G,best.B).ToArgb();
            }
            var parts=new List<Part>();
            for(int p=0;p<n;p++) {
                if(labels[p]!=0 || ((uint)pixels[p]>>24)==0) continue;
                var part=new Part {id=parts.Count+1}; int head=0,tail=0;
                queue[tail++]=p; labels[p]=part.id;
                while(head<tail) {
                    int q=queue[head++],x=q%w,y=q/w;
                    part.area++;part.sx+=x;part.sy+=y;
                    part.x0=Math.Min(part.x0,x);part.x1=Math.Max(part.x1,x);
                    part.y0=Math.Min(part.y0,y);part.y1=Math.Max(part.y1,y);
                    for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++) {
                        int xx=x+dx,yy=y+dy;
                        if(xx<0 || yy<0 || xx>=w || yy>=h) continue;
                        int j=yy*w+xx;
                        if(labels[j]==0 && ((uint)pixels[j]>>24)>0) { labels[j]=part.id;queue[tail++]=j; }
                    }
                }
                part.cell=Math.Min(1,(int)(part.sy*2.0/part.area/h))*4+Math.Min(3,(int)(part.sx*4.0/part.area/w));
                parts.Add(part);
            }
            int[] cellOf=new int[parts.Count+1]; for(int i=0;i<cellOf.Length;i++) cellOf[i]=-1;
            Rectangle[] bounds=new Rectangle[8];
            int charW=0,charH=0;
            for(int cell=0;cell<8;cell++) {
                int x0=w,y0=h,x1=-1,y1=-1,count=0;
                foreach(var part in parts) if(part.cell==cell && part.area>=8) {
                    cellOf[part.id]=cell;count++;
                    x0=Math.Min(x0,part.x0);y0=Math.Min(y0,part.y0);x1=Math.Max(x1,part.x1);y1=Math.Max(y1,part.y1);
                }
                if(count==0) throw new Exception("Empty cell "+cell);
                bounds[cell]=new Rectangle(x0,y0,x1-x0+1,y1-y0+1);
                if(cell<5) {charW=Math.Max(charW,bounds[cell].Width);charH=Math.Max(charH,bounds[cell].Height);}
                Console.WriteLine(names[cell]+": "+count+" connected pieces, "+bounds[cell]);
            }
            using(var contact=new Bitmap(1024,512,PixelFormat.Format32bppArgb))
            using(var g=Graphics.FromImage(contact)) {
                g.Clear(Color.FromArgb(221,225,229));
                for(int cell=0;cell<8;cell++) {
                    var b=bounds[cell]; int ow=(cell<5?charW:b.Width)+12,oh=(cell<5?charH:b.Height)+12;
                    int ox=(ow-b.Width)/2,oy=oh-b.Height-6;
                    using(var image=new Bitmap(ow,oh,PixelFormat.Format32bppArgb)) {
                        for(int y=b.Top;y<b.Bottom;y++) for(int x=b.Left;x<b.Right;x++) {
                            int q=y*w+x;if(cellOf[labels[q]]==cell) image.SetPixel(x-b.Left+ox,y-b.Top+oy,Color.FromArgb(pixels[q]));
                        }
                        image.Save(Path.Combine(output,names[cell]+".png"),ImageFormat.Png);
                        float scale=Math.Min(232f/ow,232f/oh);int dw=(int)(ow*scale),dh=(int)(oh*scale);
                        g.DrawImage(image,cell%4*256+(256-dw)/2,cell/4*256+(256-dh)/2,dw,dh);
                        Console.WriteLine(names[cell]+".png "+ow+"x"+oh);
                    }
                }
                contact.Save(preview,ImageFormat.Png);
            }
        }
    }
}
'@
$sheetDir = $PSScriptRoot
$resourceDir = Split-Path -Parent $sheetDir
$repoDir = [IO.Path]::GetFullPath((Join-Path $sheetDir '../../../..'))
$unityDir = Join-Path $repoDir 'Unity_Making/NKK_Project/Assets/Art/Rats/UltProps'
[ArrestSlice]::Run((Join-Path $sheetDir 'arrest.png'), $resourceDir, (Join-Path $sheetDir '_preview.png'))
$names = 'police_rat_a','police_rat_b','police_rat_cuff','lawyer_rat_point','lawyer_rat_doc','prop_handcuffs','police_tape','police_siren'
$template = [IO.File]::ReadAllText((Join-Path $unityDir 'lawyer_rat_a.png.meta'))
foreach ($name in $names) {
    Copy-Item -LiteralPath (Join-Path $resourceDir ($name + '.png')) -Destination (Join-Path $unityDir ($name + '.png'))
    $metaPath = Join-Path $unityDir ($name + '.png.meta')
    # 재실행 시 이미 등록된 GUID는 보존한다.
    if (-not (Test-Path -LiteralPath $metaPath)) {
        $meta = [regex]::Replace($template, '(?m)^guid: [0-9a-f]{32}', ('guid: ' + [Guid]::NewGuid().ToString('N')))
        [IO.File]::WriteAllText($metaPath, $meta, (New-Object System.Text.UTF8Encoding($false)))
    }
}
