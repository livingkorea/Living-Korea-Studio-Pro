# Living Korea Studio Pro - Local Whisper v1.2.0

API 유료 사용 없이 로컬 Whisper 기반으로 마이크 음성을 텍스트로 변환하도록 수정한 버전입니다.

## 변경 사항

- GitHub Actions `PublishSingleFile=false` 적용
  - Whisper native DLL이 EXE 안에 묶여 사라지는 문제 방지
- `Whisper.net.Runtime.Cpu` 적용
  - GPU/CUDA 없이 CPU 전용 로컬 Whisper 실행
- 마이크 버튼 구조 단순화
  - `마이크 시작`을 누르면 5초 단위로 자동 녹음
  - 녹음 조각마다 Whisper 변환
  - 한국어 원문 박스에 자동 타이핑
  - 변환 후 자동 번역 실행
- Whisper Runtime DLL 누락 시 더 알아보기 쉬운 오류 메시지 표시

## 사용 방법

1. GitHub에 이 프로젝트를 업로드합니다.
2. Actions에서 Build Windows EXE를 실행합니다.
3. Artifact: `LivingKoreaStudio-Whisper-v1.2.0-Local` 다운로드
4. 압축을 푼 뒤 `LivingKoreaStudio.exe` 실행

## 중요

이 버전은 EXE 하나짜리 프로그램이 아닙니다.
Whisper 실행에 필요한 DLL과 `runtimes` 폴더가 함께 있어야 합니다.

따라서 다운로드한 artifact 압축을 푼 뒤, 폴더 안 파일을 그대로 유지한 상태에서 실행하세요.

## 첫 실행

첫 실행 시 `ggml-tiny.bin` Whisper 모델을 자동 다운로드합니다.
다운로드 위치:

`C:\Users\User\AppData\Local\LivingKoreaStudio\models\ggml-tiny.bin`

