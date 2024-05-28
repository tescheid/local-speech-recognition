import socket
import json
import pvrhino
from pvrecorder import PvRecorder

ACCESS_KEY = '/7HzQ/X/b5HGImCVTIKoJvZGRfZRXMDbQ/3VJNSYxkowIRyRTasFdg=='
CONFIRMATION_CONTEXT_PATH = '/home/auxilium/netcore/LocalSpeechRecognitionMaster/Python/rhino_model/BAT_de_raspberry-pi_v3_0_0.rhn'
DATE_CONTEXT_PATH = '/home/auxilium/netcore/LocalSpeechRecognitionMaster/Python/rhino_model/Absenz_de_raspberry-pi_v3_0_0.rhn'
ACTION_CONTEXT_PATH = '/home/auxilium/netcore/LocalSpeechRecognitionMaster/Python/rhino_model/Actions_de_raspberry-pi_v3_0_0.rhn'
MODE_CONTEXT_PATH = '/home/auxilium/netcore/LocalSpeechRecognitionMaster/Python/rhino_model/ModeSelection_de_raspberry-pi_v3_0_0.rhn'
OUTPUT_JSON_PATH = '/home/auxilium/netcore/LocalSpeechRecognitionMaster/Python/speechRecognitionOutput.json'
MODEL_FILE_PATH = '/home/auxilium/netcore/LocalSpeechRecognitionMaster/Python/rhino_model/rhino_params_de.pv'

# Funktion zum Erstellen einer Rhino-Instanz
def create_rhino(context_path):
    return pvrhino.create(
        access_key=ACCESS_KEY,
        context_path=context_path,
        model_path=MODEL_FILE_PATH,
        sensitivity=0.5,
        endpoint_duration_sec=1.0,
        require_endpoint=True
    )

# Server-Socket einrichten
server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.bind(('0.0.0.0', 41000))
server_socket.listen(1)

print("Listening for connections...")

# Standardkonfiguration
rhino = create_rhino(CONFIRMATION_CONTEXT_PATH)
recorder = PvRecorder(frame_length=rhino.frame_length, device_index=15)

try:
    while True:
        try:
            conn, _ = server_socket.accept()
            print("Connection accepted.")
            recorder.start()

            while True:
                command = conn.recv(64).decode('utf-8').strip()
                if not command:
                    # Leere Eingabe beendet die Verbindung
                    print("Connection closed.")
                    break

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
                    conn.sendall(b'Start received')
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
                                print("Didn't understand the command.")
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
