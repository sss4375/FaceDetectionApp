# FaceDetectionApp

얼굴에서 **코 위치를 자동 검출**하고, 해당 위치에 **루돌프 코를 합성**하는 Windows용 WPF 애플리케이션입니다.  
이미지, 영상, 실시간 웹캠까지 지원하며, Mediapipe 기반의 Python 서버를 활용합니다.

---

## ✅ 주요 기능

- 이미지에서 얼굴 인식 후 루돌프 코 자동 합성 및 저장
- 이미지 내 **다중 얼굴 인식** 지원
- 영상 파일 처리 → 결과 영상 자동 저장 및 실시간 재생
- **웹캠 영상 실시간 얼굴 인식** 및 루돌프 코 합성
- Flask 기반 Python 서버와 통신하여 빠르고 정확한 코 검출
- PyInstaller로 빌드된 독립 실행형 Python 서버 제공

---

## 🖥️ 시스템 요구 사항

- **Windows 10 이상**
- **.NET 8.0 이상**
- **OpenCvSharp4**
- **Python 3.9 이상**  
  (단, 제공된 `nose_detection_server.exe`를 사용할 경우 Python 미설치 환경에서도 사용 가능)

---

## 🚀 실행 방법

1. 레포지토리 클론 또는 릴리즈 다운로드
2. `FaceDetectionApp.sln`을 Visual Studio로 열어 실행
3. 초기 실행 시 Python 서버(`nose_detection_server.exe`) 자동 실행됨
4. 이미지, 영상 또는 웹캠을 통해 코 위치 검출 및 루돌프 코 합성 확인

---

## ⚠️ 백신 예외 설정 안내

일부 백신(V3, 알약 등)은 PyInstaller로 빌드된 `.exe` 파일을 **오탐지**하여 차단할 수 있습니다.  
다음 절차로 예외 처리 후 사용해주세요:

1. 백신 프로그램 실행
2. 예외 설정 > 파일 또는 폴더 경로 추가
3. `nose_detection_server.exe`가 위치한 폴더 또는 파일 경로 추가
4. 프로그램 재실행

> 이 파일은 로컬에서만 동작하며, 외부 인터넷 접속은 하지 않습니다.

---

## 🔧 Python 서버 직접 빌드하기 (선택 사항)

`nose_detection_server.py`를 직접 `.exe`로 빌드하려면 다음 명령어를 사용하세요:

```bash
pyinstaller --onefile ^
--add-data "C:\Users\사용자명\venv\Lib\site-packages\mediapipe\modules\face_landmark\face_landmark.tflite;mediapipe/modules/face_landmark" ^
--add-data "C:\Users\사용자명\venv\Lib\site-packages\mediapipe\modules\face_landmark\face_landmark_front_cpu.binarypb;mediapipe/modules/face_landmark" ^
--add-data "C:\Users\사용자명\venv\Lib\site-packages\mediapipe\modules\face_detection\face_detection_short_range.tflite;mediapipe/modules/face_detection" ^
nose_detection_server.py

✅ 최소 requirements.txt
flask==3.1.1
opencv-python==4.11.0.86
mediapipe==0.10.21
numpy==1.26.4
pillow==11.2.1
