from vosk import Model, KaldiRecognizer
import pyaudio

isGerman=False

if isGerman:
    model=Model('vosk-model-small-de-0.15')
else:
    model=Model('vosk-model-small-en-us-0.15')

recognizer=KaldiRecognizer(model,16000)

# Recognize from the microfone
capturedAudio=pyaudio.PyAudio()
stream=capturedAudio.open(format=pyaudio.paInt16,channels=1,rate=16000,input=True,frames_per_buffer=8192)
stream.start_stream()

while True:
    data=stream.read(4096)
    if len(data) == 0:
        break
    if recognizer.AcceptWaveform(data):
        print(recognizer.Result())

        