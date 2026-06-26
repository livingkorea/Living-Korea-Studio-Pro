# LivingKoreaStudio v1.3.0 EXERUN

목표: GitHub Actions에서 `EXERUN` artifact가 반드시 생성되도록 workflow를 단순화했습니다.

## 사용 방법

1. 이 ZIP 안의 파일을 GitHub 저장소에 그대로 업로드/커밋합니다.
2. GitHub → Actions → `Build EXERUN` → `Run workflow` 실행.
3. 성공 후 Artifacts에서 `EXERUN` 다운로드.
4. `EXERUN.zip` 압축 해제 후 `LivingKoreaStudio.exe` 실행.

## 중요

- 로컬 무료 Whisper 방식입니다.
- `PublishSingleFile=false`로 DLL을 EXE와 함께 배포합니다.
- Actions 경고 중 Node.js 관련 경고는 보통 무시해도 됩니다.
