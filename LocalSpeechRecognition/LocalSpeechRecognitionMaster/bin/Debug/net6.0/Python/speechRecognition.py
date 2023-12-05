from __future__ import print_function

import sys
print(sys.path)
sys.path.append("/home/thgut/.local/lib/python3.9/site-packages")
from vosk import Model, KaldiRecognizer
import json

import time
import getopt
import alsaaudio
import wave

is_german=True
frame_rate=16000

if is_german:
    model=Model('/home/thgut/Desktop/LSR/Master/Python/vosk-model-small-de-0.15')
else:
    model=Model('/home/thgut/Desktop/LSR/Master/Python/vosk-model-small-en-us-0.15')

recognizer=KaldiRecognizer(model,frame_rate*2)

if __name__ == '__main__':

	device = 'hw:3,0'
	opts, args = getopt.getopt(sys.argv[1:], 'd:')
	for o, a in opts:
		if o == '-d':
			device = a
	
	# Recognize from the microfone
	inp = alsaaudio.PCM(alsaaudio.PCM_CAPTURE, alsaaudio.PCM_NONBLOCK, 
			channels=1, rate=frame_rate, format=alsaaudio.PCM_FORMAT_S16_LE, 
			periodsize=4096, device=device)

	while True:
		l, data = inp.read()
		#print(l)
		#if not l:
			#print("break")
			#break
		if l and recognizer.AcceptWaveform(data):
			result = recognizer.Result()
			print(result)
			with open(file="speechRecognitionOutput.json",mode="w") as fp:
				json.dump(obj=result,fp=fp,indent=4)


        
