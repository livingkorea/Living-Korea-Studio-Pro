# Living Korea Studio Whisper v1.2.1 Local Verified

API 유료 사용 없이 로컬 Whisper CPU Runtime 중심으로 동작하도록 구성한 버전입니다.

## 핵심 변경

- GitHub Actions 자동 빌드 검증 추가
- `PublishSingleFile=false` 고정
- `PublishTrimmed=false` 고정
- Whisper native runtime DLL 포함 여부 자동 검사
- 빌드 성공 후 검증된 ZIP artifact 자동 생성
- 프로그램 안에 `환경진단 / Diagnose` 버튼 추가

## GitHub에서 사용하는 방법

1. 이 ZIP 안의 파일을 GitHub 저장소에 업로드합니다.
2. GitHub 저장소의 `Actions` 탭으로 이동합니다.
3. `Build and Verify Windows EXE` workflow를 실행합니다.
4. 성공하면 `LivingKoreaStudio-Whisper-v1.2.1-Local-Verified` artifact를 다운로드합니다.
5. ZIP을 풀고 `LivingKoreaStudio.exe`를 실행합니다.

## 실패하면 확인할 것

Actions가 실패할 경우 대부분 아래 문제입니다.

- Whisper native DLL이 publish 폴더에 없음
- SingleFile 방식으로 잘못 빌드됨
- Runtime 패키지 복원이 실패함

이번 버전은 `scripts/Verify-Publish.ps1`이 이 문제를 자동으로 잡습니다.

## 사용자 PC에서 오류가 나면

프로그램에서 `환경진단 / Diagnose` 버튼을 누르세요.
진단 로그가 아래 위치에 저장됩니다.

```text
C:\Users\User\AppData\Local\LivingKoreaStudio\logs\diagnostics_날짜.txt
```

그 로그를 보내주면 다음을 확인할 수 있습니다.

- 마이크 장치 인식 여부
- Whisper 모델 파일 존재 여부
- native DLL 존재 여부
- 실행 폴더 경로
- .NET/OS 환경

## 참고

Whisper 음성 인식은 로컬에서 동작합니다. 단, 현재 영어 번역 기능은 기존 코드의 무료 온라인 번역 방식(MyMemory)을 유지합니다. 완전 오프라인 번역까지 원하면 별도의 로컬 번역 엔진 구조가 필요합니다.
