---
description: AGENT_HANDOFF.md 갱신 — 다음 에이전트용 인수인계 문서 작성
---

저장소의 **`docs/AGENT_HANDOFF.md`**를 최신 상태로 작성하거나 덮어씁니다.

## 절차

1. 루트 **`AGENTS.md`**(팀 규칙)를 한 번 확인하고, 핸드오프는 규칙을 반복해 장황하게 쓰지 말고 **참조만** 안내합니다.
2. **`Assets/Tarot/`** 기준으로 실제 코드·씬·플로우가 문서와 맞는지 확인합니다. (`TarotGameManager`, `TarotUIManager`, `TarotAIManager`, `TarotPromptBuilder`, `TarotCardAnimator`, `TarotCardResourceLoader` 등)
3. 문서에 포함할 요소:
   - **경로·구조** (주요 에셋·씬 위치)
   - **런타임 플로우** (사용자 액션 → AI → 카드 UI → 결과 표시, 순서와 호출 이름)
   - **주요 클래스 표** (파일 경로와 역할 한 줄)
   - **인스펙터 연결** (SerializeField로 꼭 연결해야 하는 것)
   - **패키지·빌드** (LitMotion, EditorBuildSettings, NuGet 등 이 레포에서 중요한 것만)
   - **주의**(FindObjectOfType 지양, `[점괘]` 파싱 등)

## 형식

- 제목·본문은 **한국어**로 짧고 스캔하기 쉽게 유지합니다.
- 이미 있는 섹션 구조를 유지해도 되고, 레포에 맞게 정리해도 됩니다.
- **추측으로 존재하지 않는 기능을 쓰지 마세요.** 애매하면 코드/씬을 직접 읽고 반영합니다.

## 제약

- 사용자가 명시하지 않은 파일은 수정하지 않습니다. 이번 작업에서 **`docs/AGENT_HANDOFF.md`**만 갱신합니다.
- 커밋·푸시는 사용자가 요청할 때만 수행합니다.

작업 후 변경 요약(어떤 섹션을 어떻게 바꿨는지)을 짧게 보고합니다.
