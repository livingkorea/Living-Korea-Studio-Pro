# Living Korea Studio

Windows용 유튜브 번역 메모장 프로그램입니다.

## 기능
- 한국어 입력
- 마이크 음성 입력 버튼
- 영어 자동 번역
- 유튜브 대본 스타일 변환
- 메모장 저장
- 한국어+영어 복사
- TXT 저장

## 가장 쉬운 EXE 생성 방법: GitHub 자동 빌드

1. GitHub에 새 저장소를 만듭니다.
2. 이 ZIP 안의 모든 파일을 저장소에 업로드합니다.
3. GitHub 저장소 상단의 `Actions` 탭으로 이동합니다.
4. `Build Windows EXE`를 선택합니다.
5. `Run workflow` 버튼을 누릅니다.
6. 빌드가 끝나면 아래쪽 `Artifacts`에서 `LivingKoreaStudio-Windows-EXE`를 다운로드합니다.
7. 압축을 풀면 `LivingKoreaStudio.exe`가 있습니다.

## 주의
- 이 방식은 사용자의 컴퓨터에 Python, .NET을 설치하지 않아도 GitHub가 대신 빌드합니다.
- 번역은 무료 온라인 번역 API를 사용하므로 인터넷 연결이 필요합니다.
- 마이크 기능은 Windows에 한국어 음성 인식 기능이 설치되어 있어야 정상 작동합니다.
