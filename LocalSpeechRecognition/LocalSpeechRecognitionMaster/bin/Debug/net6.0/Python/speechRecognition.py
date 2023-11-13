from vosk import Model, KaldiRecognizer
import pyaudio
import json

isGerman=True

if isGerman:
   # model=Model('/home/thgut/Desktop/LSR/Master/Python/vosk-model-small-de-0.15');
    model=Model('C:/Users/domin/Documents/Dev/PAIND/local-speech-recognition/LocalSpeechRecognition/LocalSpeechRecognitionMaster/bin/Debug/net6.0/Python/vosk-model-small-de-0.15')
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
        result = recognizer.Result()
        print(result)
        with open(file="speechRecognitionOutput.json",mode="w") as fp:
            json.dump(obj=result,fp=fp,indent=4)


        