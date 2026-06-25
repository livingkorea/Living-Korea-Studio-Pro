# Living Korea Studio Pro

한국어 음성 입력, 영어 번역, 유튜브 대본 메모장 프로그램입니다.  
Korean voice input, English translation, and YouTube creator notes.

## v1.1 변경사항 / Changes
- Windows 음성 인식 엔진 제거
- Whisper 기반 음성 인식 구조로 변경
- 마이크 녹음 → Whisper 텍스트 변환 → 영어 번역
- Windows 한국어 음성 인식 엔진(ko-KR) 설치 불필요
- 첫 음성 변환 시 Whisper 모델이 자동 다운로드됩니다.

## 중요 / Important
첫 실행 후 음성 변환을 처음 사용할 때 Whisper 모델 파일을 다운로드합니다.
인터넷 연결이 필요하며, 다운로드에 시간이 걸릴 수 있습니다.

This version downloads a Whisper model on first transcription.
Internet is required for the first use.
