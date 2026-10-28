// 찍!찍!!찍!!! MVP — 접속 국가 알려 주기 (Cloudflare Worker, 무료 요금제로 충분).
// 게임이 실행마다 한 번 GET → 국가 코드 두 글자만 돌려줌 (예 KR, US · 모르면 XX · Tor = T1).
// IP 는 Cloudflare 안에서만 쓰이고 저장하지 않음. 게임은 받은 코드만 Firestore 기록의 nation 칸에 넣음.
// 배포: Cloudflare 대시보드 → Workers & Pages → 만들기 → 이 파일 내용 붙여 넣기 → 배포 → 주소를 ServerConfig.nationUrl 에.
export default {
  async fetch(request) {
    const headers = {
      'Access-Control-Allow-Origin': '*',          // itch.io(itch.zone) WebGL 에서 부름
      'Access-Control-Allow-Methods': 'GET, OPTIONS',
      'Cache-Control': 'no-store',
      'Content-Type': 'text/plain; charset=utf-8',
    };
    if (request.method === 'OPTIONS') return new Response(null, { status: 204, headers });
    if (request.method !== 'GET') return new Response('', { status: 405, headers });
    const c = String((request.cf && request.cf.country) || 'XX').toUpperCase();
    return new Response(/^[A-Z0-9]{2}$/.test(c) ? c : 'XX', { headers });
  },
};
