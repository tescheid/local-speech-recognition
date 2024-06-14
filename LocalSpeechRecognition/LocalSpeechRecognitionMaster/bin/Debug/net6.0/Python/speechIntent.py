import socket
import json
import pvrhino
from pvrecorder import PvRecorder
import os
import time



current_dir = os.getcwd()
OUTPUT_JSON_PATH = current_dir +'/Python/speechRecognitionOutput.json'
port = 40000


# Function to create a rhino instance

CONFIRMATION_CONTEXT_PATH = current_dir +'/Python/rhino_model/BAT_de_raspberry-pi_v3_0_0.rhn'
DATE_CONTEXT_PATH = current_dir +'/Python/rhino_model/Absenz_de_raspberry-pi_v3_0_0.rhn'
ACTION_CONTEXT_PATH = current_dir +'/Python/rhino_model/Actions_de_raspberry-pi_v3_0_0.rhn'
MODE_CONTEXT_PATH = current_dir +'/Python/rhino_model/ModeSelection_de_raspberry-pi_v3_0_0.rhn'
MODEL_FILE_PATH = current_dir +'/Python/rhino_model/rhino_params_de.pv'

ACCESS_KEY = '/7HzQ/X/b5HGImCVTIKoJvZGRfZRXMDbQ/3VJNSYxkowIRyRTasFdg=='

def create_rhino(context_path):
    return pvrhino.create(
        access_key=ACCESS_KEY,
        context_path=context_path,
        model_path=MODEL_FILE_PATH,
        sensitivity=0.5,
        endpoint_duration_sec=1.0,
        require_endpoint=True
    )
# Standardkonfiguration
rhino = create_rhino(CONFIRMATION_CONTEXT_PATH)
recorder = PvRecorder(frame_length=rhino.frame_length, device_index=15)

# Server-Socket einrichten
server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
server_socket.bind(('0.0.0.0', port))
server_socket.listen(1)

print("Listening for connections...")

try:
    while True:
        try:
            conn, _ = server_socket.accept()
            
            while True:
                command = conn.recv(64).decode('utf-8').strip()
                print("Received command:", command)

                if command == "confirmation":
                    rhino = create_rhino(CONFIRMATION_CONTEXT_PATH)
                    conn.sendall(b'context switched to confirmation')
                elif command == "date":
                    rhino = create_rhino(DATE_CONTEXT_PATH)
                    conn.sendall(b'context switched to date')
                elif command == "action":
                    rhino = create_rhino(ACTION_CONTEXT_PATH)
                    conn.sendall(b'context switched to action')
                elif command == "mode":
                    rhino = create_rhino(MODE_CONTEXT_PATH)
                    conn.sendall(b'context switched to modeselection')
                    
                elif command == "start":
                    recorder.start()
                    conn.sendall(b'Start received')
                    not_understood_count = 0
                    while True:                        
                        pcm = recorder.read()
                        if rhino.process(pcm):
                            inference = rhino.get_inference()
                            if inference.is_understood:
                                result = {
                                    "intent": inference.intent,
                                    "slots": inference.slots
                                }
                                with open(OUTPUT_JSON_PATH, 'w') as output_file:
                                    json.dump(result, output_file)
                                print("Wrote to JSON.")
                                break
                            else:
                                not_understood_count += 1
                                print("Didn't understand the command.")
                                if not_understood_count == 1:
                                    result = {
                                        "intent": "noAnswer",
                                        "slots": {}
                                    }
                                    with open(OUTPUT_JSON_PATH, 'w') as output_file:
                                        json.dump(result, output_file)
                                    print("Wrote empty JSON after unsuccessful attempts.")
                                    not_understood_count = 0
                                    break
                    print("Finished.")
                else:
                    conn.sendall(b'unknown command')

        except socket.error as e:
            print(f"Socket error: {e}")
        finally:
            print("Closing connection...")
            conn.close()
            recorder.stop()

except KeyboardInterrupt:
    print("Stopping...")

finally:
    print("Stopping finally...")
    recorder.delete()
    rhino.delete()
