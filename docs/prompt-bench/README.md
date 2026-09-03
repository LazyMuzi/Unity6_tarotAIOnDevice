# 프롬프트 벤치 (Gemma 3 1B)

`TarotPromptBuilder`의 프롬프트를 바꾸기 전에, 앱과 **같은 gguf**로 여러 번 샘플링해 실패율을 확인하는 스크립트.
Unity 없이 ollama만으로 돈다.

```bash
# 1) 앱이 쓰는 gguf를 ollama에 그대로 등록(1회). 채팅 템플릿은 gguf 내장값을 쓴다.
printf 'FROM %s/.config/LLMUnity/models/gemma-3-4b-it-Q4_K_M.gguf\n' "$HOME" > /tmp/Modelfile
ollama create tarot-gemma4b -f /tmp/Modelfile   # 기본 모델. 1B는 tarot-gemma1b로 만들고 TAROT_BENCH_MODEL=tarot-gemma1b

# 2) 변형 파일을 N회(기본 12) 샘플링. 세 번째 인자는 시드 시작값, -v는 정상 응답도 출력.
python3 docs/prompt-bench/bench.py docs/prompt-bench/variants/spread_current.py 30 7000 -v
python3 docs/prompt-bench/bench.py docs/prompt-bench/variants/daily_current.py 18
```

변형 파일은 `build(concern, cards, ors) -> (system, user)` 하나만 구현하면 된다(`concern.text`/`.label`/`.area`).
`variants/*_current.py`는 **C# 프롬프트와 문자열이 같아야** 하므로 `TarotPromptBuilder`를 고치면 함께 고친다.
`bench.py`의 `extract`는 `TarotGameManager.ExtractReadingResult`의 라벨 처리 규칙(에코 줄 삭제 / 구간 라벨 벗김)을 미러링한다.

플래그 뜻: `EMPTY` 후처리 후 빈 결과(앱에서 "해석하지 못했습니다") · `OFF-TOPIC` 고민의 핵심어(`CONCERNS`의 anchors)가 답에 하나도 없음 · `LABEL` 라벨 잔존 · `HASIPSIO`/`BANMAL`/`NOT-HAEYO` 말투 이탈 · `TRUNCATED` numPredict 컷 · `MARKDOWN/EMOJI`.

기록(2026-09-03, 샘플링 파라미터는 `TarotAIManager`와 동일):

| 변형 | 모델 | 결과 |
|------|------|------|
| `spread_before_2026-09.py` (금지문 방식) | 1B | 12/12 `EMPTY` — 모델이 `본문:/조언:/결론:`으로만 답하고 후처리가 그 줄을 전부 삭제 |
| `spread_2026-09-03_a.py` (템플릿+예시, `분야 =` 줄 포함) | 1B / 4B | 파싱은 전부 성공하지만 `OFF-TOPIC` 8/16, 9/16 — "점심 뭐 먹을지"에 "중요한 결정" 얘기 |
| `spread_current.py` ("답 먼저" 4줄, 분야 줄 제거, 고민을 끝에 두고 재인용) | 4B | 24/24 |
| `daily_current.py` (같은 원칙, 2줄) | 4B | 15/16 (남은 1건은 `합니다` 말투) |

오프토픽 원인과 해결(실측):
- `분야 = 갈림길, 선택, 결정` 같은 카테고리 설명 줄이 고민보다 세게 작용해 추상어를 유도 → **분야 줄 제거**.
- 고민이 프롬프트 중간에 묻힘 → **고민을 맨 끝에 두고**, 마지막 줄에서 `'…' 이 고민에 대해` 로 한 번 더 인용.
- 예시에 `고민 = …` 줄을 넣으면 모델이 그 줄을 베끼다가 고민을 다른 내용으로 바꿔 적음 → 예시 제목에 괄호로만 표기.
- 첫 줄을 `답:`(고민에 대한 직접 답)으로 시작시키면 뒤 세 줄이 고민을 벗어나지 않음. 넷 중 하나는 고민 문장을 되풀이하므로 `DropLeadingConcernRestatement`가 화면에서 뺌.
