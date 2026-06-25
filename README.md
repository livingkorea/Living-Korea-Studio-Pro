# Living Korea Studio Pro

한국어 음성 입력, 영어 번역, 유튜브 대본 메모장 프로그램입니다.  
Korean voice input, English translation, and YouTube creator notes.

## v1.1.1 수정사항 / Fixes
- v1.1 빌드 오류 수정: `SetStatus()` 함수 누락 문제 해결
- Whisper 기반 음성 인식 구조 유지
- Windows 한국어 음성 인식 엔진(ko-KR) 설치 불필요

## 중요 / Important
첫 음성 변환 시 Whisper 모델 파일을 자동 다운로드합니다.
인터넷 연결이 필요하며, 첫 다운로드에는 시간이 걸릴 수 있습니다.
