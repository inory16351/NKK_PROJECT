"""MVP 서버 기록(Firestore rankups · milestones)을 접속 국가(nation)별로 모아 보기.

준비: gcloud 에 nkk-mvp 프로젝트 권한이 있는 구글 계정으로 로그인 (한 번)
    gcloud auth login
쓰기:
    python Tools/server_stats.py              # 터미널 표 + 결과 창(HTML)을 브라우저로 염
    python Tools/server_stats.py --no-html    # 터미널 표만
    python Tools/server_stats.py --since 2026-10-21   # 이 날(UTC)부터 기록만

플레이어 = uid 하나. 국가는 그 플레이어의 가장 최근 기록 값 (nation 이 없는 옛 빌드 기록은 "(없음)").
읽기 전용 — 서버 기록을 바꾸지 않음.
"""
import argparse
import html
import json
import os
import shutil
import statistics
import subprocess
import sys
import tempfile
import urllib.error
import urllib.request
import webbrowser
from collections import defaultdict

PAGE = 1000


def token():
    gcloud = shutil.which("gcloud") or shutil.which("gcloud.cmd")
    if not gcloud:
        sys.exit("gcloud 가 없음 (Google Cloud SDK 설치 후 gcloud auth login)")
    r = subprocess.run([gcloud, "auth", "print-access-token"], capture_output=True, text=True)
    if r.returncode != 0:
        sys.exit("gcloud 토큰 실패 — gcloud auth login 먼저\n" + r.stderr)
    return r.stdout.strip()


def value(v):
    for k in ("stringValue", "timestampValue"):
        if k in v:
            return v[k]
    if "integerValue" in v:
        return int(v["integerValue"])
    if "doubleValue" in v:
        return v["doubleValue"]
    return None


def fetch(project, tok, collection, since):
    """컬렉션 전체를 이름순으로 PAGE 개씩 읽음 → [{칸: 값}]"""
    url = f"https://firestore.googleapis.com/v1/projects/{project}/databases/(default)/documents:runQuery"
    out, last = [], None
    while True:
        q = {"from": [{"collectionId": collection}], "orderBy": [{"field": {"fieldPath": "__name__"}}], "limit": PAGE}
        if since:
            q["where"] = {"fieldFilter": {"field": {"fieldPath": "at"}, "op": "GREATER_THAN_OR_EQUAL",
                                          "value": {"timestampValue": since + "T00:00:00Z"}}}
            q["orderBy"] = [{"field": {"fieldPath": "at"}}, {"field": {"fieldPath": "__name__"}}]
        if last:
            q["startAt"] = {"values": last, "before": False}
        rq = urllib.request.Request(url, json.dumps({"structuredQuery": q}).encode(), method="POST", headers={
            "Authorization": "Bearer " + tok, "Content-Type": "application/json", "x-goog-user-project": project})
        try:
            with urllib.request.urlopen(rq, timeout=60) as res:
                rows = json.load(res)
        except urllib.error.HTTPError as e:
            sys.exit(f"{collection} 읽기 실패 {e.code}: {e.read().decode(errors='replace')[:500]}")
        docs = [r["document"] for r in rows if "document" in r]
        for d in docs:
            out.append({k: value(v) for k, v in d.get("fields", {}).items()})
        if len(docs) < PAGE:
            return out
        d = docs[-1]
        last = ([d["fields"]["at"]] if since else []) + [{"referenceValue": d["name"]}]


def build(rankups, milestones):
    players = {}  # uid → {nation, at, tier, floor, rats}

    def see(r, rats=False):
        uid = r.get("uid")
        if not uid:
            return
        p = players.setdefault(uid, {"nation": None, "at": "", "tier": 1, "floor": 1, "rats": False})
        at = r.get("at") or ""
        if at >= p["at"]:
            p["at"] = at
            p["nation"] = r.get("nation") or p["nation"]
        elif not p["nation"]:
            p["nation"] = r.get("nation")
        p["tier"] = max(p["tier"], r.get("tier") or 1)
        p["floor"] = max(p["floor"], r.get("maxFloor") or 1)
        p["rats"] = p["rats"] or rats

    for r in milestones:
        see(r, rats=r.get("feature") == "rats")
    for r in rankups:
        see(r)

    by = defaultdict(list)
    for p in players.values():
        by[p["nation"] or "(없음)"].append(p)
    rows = []
    for nation, ps in by.items():
        rows.append({
            "nation": nation,
            "players": len(ps),
            "rats": sum(p["rats"] for p in ps),
            "tier2": sum(p["tier"] >= 2 for p in ps),
            "tierAvg": statistics.mean(p["tier"] for p in ps),
            "floorMed": statistics.median(p["floor"] for p in ps),
            "floorMax": max(p["floor"] for p in ps),
        })
    rows.sort(key=lambda r: (-r["players"], r["nation"]))
    return rows, len(players)


COLS = [("nation", "nation"), ("players", "플레이어"), ("share", "비율"), ("rats", "쳇바퀴 해금"),
        ("tier2", "훈장 2+"), ("tierAvg", "평균 훈장"), ("floorMed", "최고 층 중앙값"), ("floorMax", "최고 층 최대")]


def cells(r, total):
    return [r["nation"], str(r["players"]), f"{r['players'] / total:.0%}" if total else "-", str(r["rats"]),
            str(r["tier2"]), f"{r['tierAvg']:.1f}", f"{r['floorMed']:g}", str(r["floorMax"])]


def print_table(rows, total):
    head = [c[1] for c in COLS]
    body = [cells(r, total) for r in rows]
    w = [max(len(h), *(len(b[i]) for b in body)) if body else len(h) for i, h in enumerate(head)]
    print("  ".join(h.ljust(w[i]) for i, h in enumerate(head)))
    for b in body:
        print("  ".join(x.ljust(w[i]) for i, x in enumerate(b)))
    print(f"\n플레이어 합계 {total}")


def write_html(rows, total, info):
    trs = "".join("<tr>" + "".join(f"<td>{html.escape(x)}</td>" for x in cells(r, total)) + "</tr>" for r in rows)
    ths = "".join(f"<th>{html.escape(c[1])}</th>" for c in COLS)
    page = f"""<!doctype html><meta charset="utf-8"><title>NKK 서버 통계 (국가별)</title>
<style>
body{{font-family:system-ui,'Malgun Gothic',sans-serif;margin:24px;background:#fff;color:#222}}
table{{border-collapse:collapse}}th,td{{border:1px solid #ccc;padding:6px 12px;text-align:right}}
th{{background:#f3f3f3}}td:first-child,th:first-child{{text-align:left;font-weight:600}}
p{{color:#666}}
</style>
<h2>NKK 서버 통계 — 국가별</h2>
<p>{html.escape(info)} · 플레이어 합계 {total}</p>
<table><tr>{ths}</tr>{trs}</table>"""
    path = os.path.join(tempfile.gettempdir(), "nkk_server_stats.html")
    with open(path, "w", encoding="utf-8") as f:
        f.write(page)
    return path


def main():
    ap = argparse.ArgumentParser(description="MVP 서버 기록 국가별 통계")
    ap.add_argument("--project", default="nkk-mvp")
    ap.add_argument("--since", help="YYYY-MM-DD (UTC) 부터 기록만")
    ap.add_argument("--no-html", action="store_true", help="결과 창(HTML) 열지 않음")
    a = ap.parse_args()

    tok = token()
    rankups = fetch(a.project, tok, "rankups", a.since)
    milestones = fetch(a.project, tok, "milestones", a.since)
    rows, total = build(rankups, milestones)
    info = f"rankups {len(rankups)}개 · milestones {len(milestones)}개" + (f" · {a.since} 부터" if a.since else "")
    print(info + "\n")
    print_table(rows, total)
    if not a.no_html:
        path = write_html(rows, total, info)
        print("결과 창:", path)
        webbrowser.open("file:///" + path.replace("\\", "/"))


if __name__ == "__main__":
    main()
