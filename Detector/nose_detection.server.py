from flask import Flask, request, jsonify
import cv2
import numpy as np
import mediapipe as mp
from io import BytesIO
from PIL import Image

app = Flask(__name__)
mp_face_mesh = mp.solutions.face_mesh
face_mesh = mp_face_mesh.FaceMesh(static_image_mode=True, max_num_faces=30)

@app.route('/detect', methods=['POST'])
def detect():
    if 'file' not in request.files:
        return jsonify({'error': 'No file part'}), 400

    file = request.files['file']
    if file.filename == '':
        return jsonify({'error': 'No selected file'}), 400

    try:
        image = Image.open(BytesIO(file.read())).convert("RGB")
        image_np = np.array(image)
        results = face_mesh.process(image_np)

        noses = []
        if results.multi_face_landmarks:
            for face_landmarks in results.multi_face_landmarks:
                h, w, _ = image_np.shape

                tip = face_landmarks.landmark[1]
                left = face_landmarks.landmark[98]
                right = face_landmarks.landmark[327]

                x = int(tip.x * w)
                y = int(tip.y * h)
                width = int(((right.x - left.x) * w) * 1.2)

                noses.append({'x': x, 'y': y, 'width': width})

        return jsonify({'noses': noses})

    except Exception as e:
        return jsonify({'error': str(e)}), 500

if __name__ == '__main__':
    app.run(port=5001)
