# 새 다운로드 파일이 생기면 크기가 멈출 때 바로 복사: grab.sh <출력 경로>
out=$1; start=$(date +%s)
before=$(ls ~/Downloads)
while true; do
  for f in ~/Downloads/*; do
    n=$(basename "$f"); echo "$before" | grep -qxF "$n" && continue
    s1=$(stat -c %s "$f" 2>/dev/null); sleep 0.4; s2=$(stat -c %s "$f" 2>/dev/null)
    if [ -n "$s1" ] && [ "$s1" = "$s2" ] && [ "$s1" -gt 1000 ]; then cp "$f" "$out" && echo "grabbed $n $s1" && exit 0; fi
  done
  [ $(( $(date +%s) - start )) -gt 60 ] && echo timeout && exit 1
  sleep 0.2
done
