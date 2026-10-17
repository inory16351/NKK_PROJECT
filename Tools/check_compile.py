"""유니티 없이 스크립트 컴파일 확인 (2026-10-17).
Assembly-CSharp.csproj 를 복사해 Compile 을 Assets/Scripts/**/*.cs (Editor 폴더 제외) 로 바꾸고, 출력은 임시 폴더로 → dotnet build.
사용: python Tools/check_compile.py   (dotnet SDK 필요, 유니티가 csproj 를 한 번은 만들어 둬야 함)"""
import os, subprocess, sys, tempfile

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Unity_Making', 'NKK_Project')
tmp = tempfile.mkdtemp(prefix='nkk_build_').replace('\\', '/')
t = open(os.path.join(ROOT, 'Assembly-CSharp.csproj'), encoding='utf-8-sig').read()
t = '\n'.join(l for l in t.split('\n') if 'Compile Include' not in l)
t = t.replace(r'<BaseIntermediateOutputPath>Temp\obj\$(MSBuildProjectName)</BaseIntermediateOutputPath>', f'<BaseIntermediateOutputPath>{tmp}/obj/</BaseIntermediateOutputPath>')
t = t.replace(r'<OutputPath>Temp\bin\Debug\</OutputPath>', f'<OutputPath>{tmp}/bin/</OutputPath>')
t = t.replace('<ItemGroup>', '<ItemGroup>\n    <Compile Include="Assets/Scripts/**/*.cs" Exclude="Assets/Scripts/**/Editor/**" />', 1)
proj = os.path.join(ROOT, '_check.csproj')
open(proj, 'w', encoding='utf-8').write(t)
try:
    out = subprocess.run(['dotnet', 'build', proj, '-nologo', '-v', 'q'], capture_output=True, text=True, encoding='utf-8', errors='replace').stdout
finally:
    os.remove(proj)
errs = sorted({l.strip() for l in out.splitlines() if ': error ' in l})
for e in errs: print(e)
print('오류', len(errs), '개')
sys.exit(1 if errs else 0)
