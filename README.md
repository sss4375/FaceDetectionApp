# FaceDetectionApp - 루돌프 코 합성기 🎅

AI를 활용해 얼굴에서 **코 위치를 자동 검출**하고, 해당 위치에 **루돌프 코를 합성**하는 Windows용 WPF 애플리케이션입니다.  
이미지와 영상, 실시간 웹캠까지 지원하며, Mediapipe 기반의 Python 서버를 활용합니다.

---

## ✅ 주요 기능

- 이미지에서 얼굴 인식 후 루돌프 코 자동 합성 및 저장
- 영상 파일 처리 → 결과 영상 자동 저장 및 실시간 재생
- 웹캠 영상 실시간 얼굴 인식 및 루돌프 코 합성
- Flask 기반 Python 서버와 통신하여 빠르고 정확한 코 검출
- PyInstaller로 빌드된 독립 실행형 Python 서버 포함

---

## 🖥️ 시스템 요구 사항

- Windows 10 이상
- .NET 8.0 이상
- OpenCvSharp4
- Python 3.9 이상 (Flask + Mediapipe 기반 서버)
  - 단, 제공된 `.exe` 서버를 사용할 경우 Python 설치 필요 없음

---

## 📁 프로젝트 구조
FaceDetectionApp/
├── Detector/
│ └── nose_detection_server.exe # Python 서버 실행 파일
├── Resources/
│ ├── rudolph_nose.png # 루돌프 코 이미지
│ ├── default_idle.jpg # 기본 이미지
│ └── webcam_loading.jpg # 웹캠 로딩 중 이미지
├── result/
│ └── *_rudolph.jpg / *_rudolph.mp4 # 처리 결과 저장 폴더
├── ViewModels/
│ └── MainViewModel.cs # 주요 로직
├── Models/
│ └── FaceDetectionModel.cs # (옵션) 데이터 모델
├── Helpers/
│ └── NoseDetectorClient.cs # Python 서버 통신 클라이언트
├── MainWindow.xaml
└── MainWindow.xaml.cs


---

## 🚀 실행 방법

1. **프로그램 실행**
   - Visual Studio에서 `FaceDetectionApp` 실행
   - 또는 빌드 후 배포 실행파일 실행

2. **기능 사용**
   - 이미지/영상 불러오기 → 결과 자동 저장 (`result/`)
   - 웹캠 실행 → 실시간으로 루돌프 코 합성 표시
   - 영상 저장 기능은 자동 저장됨 (입력명_rudolph.mp4)

3. **웹캠 인덱스**
   - 기본값은 `0`번. 다른 장치를 사용할 경우 수동으로 인덱스 수정 필요

---

## 🌐 Python 서버 구성 (선택 사항)

직접 Python 서버를 실행하고 싶다면:

```bash
# 가상환경 추천
pip install flask mediapipe opencv-python

python nose_detection_server.py

