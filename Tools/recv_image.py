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

    def do_OPTIONS(self):
        self.send_response(204); self._cors(); self.end_headers()

    def do_POST(self):
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
