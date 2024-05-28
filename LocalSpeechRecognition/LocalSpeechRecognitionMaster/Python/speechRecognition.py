from __future__ import print_function

import sys
import json
import time
import getopt
import alsaaudio
import wave
import socket
import sounddevice as sd
import queue
print(sys.path)
sys.path.append("/home/auxilium/LSR/lib/python3.11/site-packages")
from vosk import Model, KaldiRecognizer


is_german=True
frame_rate=16000
q = queue.Queue()

if is_german:
    model=Model('/home/auxilium/netcore/LocalSpeechRecognitionMaster/Python/vosk-model-small-de-0.15')
    
else:
    model=Model('/home/auxilium/netcore/LocalSpeechRecognitionMaster/Python/vosk-model-small-en-us-0.15')


def callback(indata, frames, time, status):
    if status:
        print(status, file=sys.stderr)
    q.put(bytes(indata))

# Funktion zur Spracherkennung
def recognize_speech(device):
    try:  
        #inp = alsaaudio.PCM(alsaaudio.PCM_CAPTURE, alsaaudio.PCM_NONBLOCK, 
        #                    channels=1, rate=frame_rate, format=alsaaudio.PCM_FORMAT_S16_LE, 
        #                   periodsize=4096, device=device)
        with sd.RawInputStream(samplerate=frame_rate, blocksize=8000, device=device,
                           dtype='int16', channels=1, callback=callback):
            recognizer = KaldiRecognizer(model, frame_rate)
            recognizer.SetWords(True)
            phrase_list = ["ja", "nein"]

            recognizer.SetGrammar(json.dumps(phrase_list))

            print("Recognition started...")
    
            while True:
                data = q.get()
                # if not l:
                #     print("no length")
                #     continue
                if recognizer.AcceptWaveform(data):
                        result = recognizer.FinalResult()
                        result_json = json.loads(result)
                        if 'text' in result_json:
                            output_dict = {"text": result_json['text']}
                            print(output_dict)  
                            with open("speechRecognitionOutput.json", mode="w") as fp:
                                json.dump(output_dict, fp)
                                return
    except Exception as e:
        print(f"Error during recognition: {e}")
    finally:
        print("Recognition finished")
        #inp.close()  # Ressourcenfreigabe

# Server-Setup
while True:
    try:    
        server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server_socket.bind(('localhost', 12345))
        server_socket.listen(1)
        print("Waiting for a connection...")
        connection, address = server_socket.accept()
 
        try:
            with connection:
                print(f"Connected by {address}")
                while True:
                    data = connection.recv(16)
                    if not data:
                        print("no data received")
                        break
                    if data.decode('utf-8') == 'start':
                        print("Start Received")
                        device = 'hw:3,0'
                        time.sleep(0.7)
                        recognize_speech(device)
        except Exception as e:
            print(f"Error during communication: {e}")
    except socket.error as e:
        print(f"Socket error: {e}")
    finally:
        server_socket.close()




        
