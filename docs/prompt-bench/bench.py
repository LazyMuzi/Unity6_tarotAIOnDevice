#!/usr/bin/env python3
"""앱과 같은 gguf를 ollama로 띄워 프롬프트 변형을 N회 샘플링하고,
TarotGameManager.ExtractReadingResult와 같은 규칙으로 실패를 분류한다. 사용법은 README.md."""
import json, os, re, sys, random, urllib.request, importlib.util
from collections import namedtuple

# 앱이 쓰는 모델과 맞춘다. 다른 모델로 재보려면 TAROT_BENCH_MODEL 환경변수.
MODEL = os.environ.get("TAROT_BENCH_MODEL", "tarot-gemma4b")
OPTS = dict(temperature=0.35, top_p=0.9, top_k=40, min_p=0.05,
            repeat_penalty=1.1, repeat_last_n=64, num_predict=200, seed=0)

CARDS = [
    ("바보", "새로운 시작, 순수함, 잠재력, 모험"),
    ("마법사", "창조력, 자신감, 잠재력 실현, 재능"),
    ("고위 여사제", "직관, 신비, 내면의 지혜, 통찰력"),
    ("여황제", "풍요, 모성, 자연, 창조"),
    ("황제", "권위, 구조, 안정, 부성"),
    ("교황", "전통, 가르침, 신념, 소속감"),
    ("연인", "사랑, 조화, 관계, 선택"),
    ("전차", "전진, 통제, 승리, 의지"),
    ("힘", "용기, 인내, 동정심, 내면의 힘"),
    ("은둔자", "자기 성찰, 고독, 내면의 탐구, 지혜"),
    ("운명의 수레바퀴", "운명, 전환점, 순환, 행운"),
    ("정의", "공정, 진실, 균형, 인과"),
    ("매달린 사람", "희생, 정지, 관점 전환, 기다림"),
    ("죽음", "끝, 변화, 재탄생, 놓아줌"),
    ("절제", "균형, 조화, 인내, 절제"),
    ("악마", "집착, 유혹, 속박, 물질주의"),
    ("탑", "급변, 붕괴, 충격, 깨달음"),
    ("별", "희망, 영감, 치유, 낙관"),
    ("달", "불안, 환상, 직관, 무의식"),
    ("태양", "성공, 활력, 기쁨, 명료함"),
    ("심판", "각성, 결산, 부활, 결단"),
    ("세계", "완성, 성취, 통합, 여행의 끝"),
]
Concern = namedtuple("Concern", "label area text anchors")

# anchors: 답이 이 고민을 실제로 다뤘다고 볼 수 있는 단어들. 하나도 없으면 OFF-TOPIC.
# 가벼운 고민(점심·주말·쇼핑)은 1B 모델이 거대한 인생 얘기로 흘러가기 쉬워 반드시 포함한다.
CONCERNS = [
    Concern("연애·관계", "연애, 애정, 가족·친구 등 사람과의 관계",
            "헤어진 연인이 자꾸 생각나요. 다시 연락해도 될까요?",
            ["연락", "연인", "재회", "헤어", "사랑", "관계", "그 사람", "추억", "잊"]),
    Concern("직장·학업", "직장, 진로, 학업, 업무",
            "이직을 준비 중인데 지금 회사를 그만둬도 될지 고민돼요",
            ["이직", "회사", "직장", "퇴사", "일자리", "업무", "커리어", "진로"]),
    Concern("금전", "돈, 재정, 물질적 안정",
            "적금을 깨서 주식에 투자하고 싶은데 괜찮을까요",
            ["적금", "주식", "투자", "돈", "자금", "재정", "저축"]),
    Concern("건강", "몸과 마음의 건강, 컨디션",
            "요즘 잠을 잘 못 자고 무기력해요",
            ["잠", "수면", "몸", "건강", "체력", "기운", "무기력", "피로", "휴식"]),
    Concern("선택·결정", "갈림길, 선택, 결정",
            "점심 뭐 먹을지 고민이에요",
            ["점심", "메뉴", "밥", "식사", "먹", "음식", "입맛"]),
    Concern("기타", "위에 해당하지 않는 기타 고민",
            "주말에 뭐 하고 놀지 고민이에요",
            ["주말", "놀", "쉬", "나들이", "약속", "취미", "휴일"]),
    Concern("선택·결정", "갈림길, 선택, 결정",
            "새 휴대폰을 살까 말까 고민돼요",
            ["휴대폰", "폰", "기기", "구매", "지출", "사는", "살까", "바꾸"]),
    Concern("기타", "위에 해당하지 않는 기타 고민",
            "이번 주에 머리 스타일을 바꿔볼까요?",
            ["머리", "스타일", "미용", "헤어", "모습", "외모"]),
]

ECHO = ["고민", "분야", "카드", "키워드", "해석 지침", "방향", "리딩 종류", "선택된 카드", "슬롯"]
BODY = ["답", "과거", "현재", "미래", "지금", "점괘", "본문", "조언", "결론", "요약"]
SYM = re.compile(r"[\U00010000-\U0010FFFF☀-➿⬀-⯿​-‏️]")


def compact(s):
    return "".join(ch for ch in s if ch.isalnum())


def extract(raw, prompt, concern_text=""):
    """Mirror of TarotGameManager.ExtractReadingResult."""
    r = raw
    if r.startswith(prompt):
        r = r[len(prompt):].lstrip()
    i = r.rfind("[점괘]")
    if i != -1:
        r = r[i + len("[점괘]"):].lstrip()
    lines = []
    for ln in r.split("\n"):
        t = ln.strip().lstrip("*").strip()
        if t in ("[생각]", "[점괘]"):
            continue
        esep = min((i for i in (t.find(c) for c in ":：=") if i >= 0), default=-1)
        if esep > 0 and any(t[:esep].strip().startswith(p) for p in ECHO):
            continue
        sep = min((i for i in (t.find(c) for c in ":：") if i >= 0), default=-1)
        if sep > 0 and t[:sep].strip().rstrip("*").strip() in BODY:
            ln = t[sep + 1:].lstrip()
        lines.append(ln)
    r = "\n".join(lines).strip()
    if not r:
        return None
    # 첫 줄이 고민의 되풀이면 제거(DropLeadingConcernRestatement 미러).
    first, _, rest = r.partition("\n")
    a, b = compact(first), compact(concern_text)
    if a and b and (b in a or a in b):
        r = rest.strip()
        if not r:
            return None
    for c in "*#_~`":
        r = r.replace(c, "")
    r = SYM.sub("", r)
    return r.strip()


def chat(system, user, seed, num_predict):
    body = {"model": MODEL, "stream": False,
            "messages": [{"role": "system", "content": system}, {"role": "user", "content": user}],
            "options": dict(OPTS, seed=seed, num_predict=num_predict)}
    req = urllib.request.Request("http://127.0.0.1:11434/api/chat", json.dumps(body).encode(),
                                 {"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=300) as resp:
        d = json.loads(resp.read())
    return d["message"]["content"], d.get("done_reason"), d.get("eval_count")


def classify(raw, out, done, concern):
    flags = []
    if out is None:
        flags.append("EMPTY")
        return flags
    if done == "length":
        flags.append("TRUNCATED")
    if re.search(r"[\*#]|[\U0001F300-\U0001FAFF☀-➿]", raw):
        flags.append("MARKDOWN/EMOJI")
    if re.search(r"^\s*(고민|분야|카드\d?|답|과거|현재|미래|본문|조언|결론)\s*[:：]", out, re.M):
        flags.append("LABEL")
    if re.search(r"\[(과거|현재|미래|점괘|생각)\]", out):
        flags.append("TAG")
    if re.search(r"[A-Za-z]{4,}", out):
        flags.append("LATIN")
    if re.search(r"[぀-ヿ一-鿿]", out):
        flags.append("CJK-FOREIGN")
    sents = [s for s in re.split(r"(?<=[.!?。])\s+", out.strip()) if s.strip()]
    if len(sents) > 6:
        flags.append(f"LONG({len(sents)})")
    if not re.search(r"(요|죠|네요|에요|예요)[.!?]?\s*$", out.strip()) and not re.search(r"(요|죠|네요|에요|예요)[.!?]", out):
        flags.append("NOT-HAEYO")
    if re.search(r"(습니다|입니다|합니다)[.!?]", out):
        flags.append("HASIPSIO")
    if re.search(r"(한다|이다|된다|있다|없다)[.!?]", out):
        flags.append("BANMAL")
    if not any(a in out for a in concern.anchors):
        flags.append("OFF-TOPIC")
    return flags


def main():
    variant = importlib.util.spec_from_file_location("v", sys.argv[1])
    v = importlib.util.module_from_spec(variant)
    variant.loader.exec_module(v)
    n = int(sys.argv[2]) if len(sys.argv) > 2 else 12
    show = "-v" in sys.argv
    rnd = random.Random(42)
    fails = 0
    tally = {}
    for k in range(n):
        concern = CONCERNS[k % len(CONCERNS)]
        cards = rnd.sample(CARDS, 3)
        ors = [rnd.choice(["정방향", "역방향"]) for _ in range(3)]
        system, user = v.build(concern, cards, ors)
        seed = int(sys.argv[3]) + k if len(sys.argv) > 3 and sys.argv[3].isdigit() else 1000 + k
        raw, done, cnt = chat(system, user, seed, getattr(v, "NUM_PREDICT", 200))
        out = extract(raw, user, concern.text)
        flags = classify(raw, out, done, concern)
        if flags:
            fails += 1
        for f in flags:
            tally[f.split("(")[0]] = tally.get(f.split("(")[0], 0) + 1
        print(f"--- #{k} [{concern.text[:14]}] {cards[0][0]}({ors[0]}) / {cards[1][0]}({ors[1]}) / {cards[2][0]}({ors[2]}) tokens={cnt} flags={flags}")
        if show or flags:
            print("RAW>", raw.replace("\n", "\\n"))
            if out and out != raw.strip():
                print("OUT>", out.replace("\n", "\\n"))
    print(f"\n== {sys.argv[1]} on {MODEL}: {n - fails}/{n} clean; {tally}")


if __name__ == "__main__":
    main()
