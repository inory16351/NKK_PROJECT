# 브라우저(ChatGPT 페이지)에서 POST 로 보낸 그림을 받아 저장하는 임시 서버 (localhost 전용)
# 사용: python Tools/recv_image.py <저장 폴더> [포트]   →  POST http://127.0.0.1:<포트>/<파일이름>  (본문 = 그림 바이트)
import os, sys, re
from http.server import BaseHTTPRequestHandler, HTTPServer

OUT = sys.argv[1]
PORT = int(sys.argv[2]) if len(sys.argv) > 2 else 8765
os.makedirs(OUT, exist_ok=True)


class H(BaseHTTPRequestHandler):
    def _cors(self):
        self.send_header('Access-Control-Allow-Origin', 'https://chatgpt.com')
        self.send_header('Access-Control-Allow-Methods', 'POST, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type')
        self.send_header('Access-Control-Allow-Private-Network', 'true')

    # GET / = 받는 페이지: 다른 탭(ChatGPT)이 window.open 으로 열고 postMessage({name, blob}) 로 그림을 넘기면 같은 출처라 바로 POST
    # (ChatGPT 페이지의 CSP 가 localhost fetch 를 막아서, 2026-10-21)
    def do_GET(self):
        page = b"""<!doctype html><meta charset=utf-8><title>recv</title><body>waiting<script>
window.addEventListener('message', async e => {
  const d = e.data || {}; if (!d.name || !d.blob) return;
  const r = await fetch('/' + d.name, {method: 'POST', body: d.blob});
  document.body.textContent += ' | ' + d.name + ' ' + r.status;
  if (e.source) e.source.postMessage({saved: d.name, status: r.status}, '*');
});
</script>"""
        self.send_response(200); self.send_header('Content-Type', 'text/html; charset=utf-8'); self.end_headers(); self.wfile.write(page)

    def do_OPTIONS(self):
        self.send_response(204); self._cors(); self.end_headers()

    # POST /upload = form 제출(multipart) 로 여러 장 한꺼번에. 앱 내장 브라우저는 창이 하나라 window.open 이 같은 탭을 덮어써서 (2026-10-21)
    def _multipart(self):
        from email.parser import BytesParser
        from email.policy import default
        n = int(self.headers.get('Content-Length', 0))
        msg = BytesParser(policy=default).parsebytes(
            b'Content-Type: ' + self.headers['Content-Type'].encode() + b'\r\n\r\n' + self.rfile.read(n))
        saved = []
        for part in msg.iter_parts():
            name = os.path.basename(part.get_filename() or '')
            if not re.fullmatch(r'ending_0[1-5]\.png', name): continue
            data = part.get_payload(decode=True)
            with open(os.path.join(OUT, name), 'wb') as f: f.write(data)
            print('saved', name, len(data), flush=True)
            saved.append(f'{name} {len(data)}')
        self.send_response(200); self.send_header('Content-Type', 'text/plain; charset=utf-8'); self.end_headers()
        self.wfile.write(('saved: ' + ', '.join(saved)).encode())

    def do_POST(self):
        if self.path.startswith('/upload'): return self._multipart()
        name = os.path.basename(self.path.strip('/'))
        if not re.fullmatch(r'ending_0[1-5]\.png', name):
            self.send_response(400); self._cors(); self.end_headers(); return
        n = int(self.headers.get('Content-Length', 0))
        data = self.rfile.read(n)
        with open(os.path.join(OUT, name), 'wb') as f: f.write(data)
        print('saved', name, len(data), flush=True)
        self.send_response(200); self._cors(); self.end_headers(); self.wfile.write(b'ok')

    def log_message(self, *a): pass


HTTPServer(('127.0.0.1', PORT), H).serve_forever()
