import socket
import sys
import sqlite3
import json
import time


def main():
    with open("settings.json") as settings_file:
        settings = json.load(settings_file)[0]

    # Create a TCP/IP socket
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    # Bind the socket to the port
    server_address = ('localhost', int(settings["PORT"]))

    print("Waiting to bind port " + str(settings["PORT"]))
    print("If it takes too long check for any process already using this port")

    # wait until used port becomes free
    connected = False
    while not connected:
        try:
            sock.bind(server_address)
            connected = True
        except:
            time.sleep(1)
            pass

    print('starting listening on %s port %s' % server_address)

    # database connection
    db_conn = sqlite3.connect(settings["DB_SERVER"])
    cursor = db_conn.cursor()

    # Listen for incoming connections
    sock.listen(settings["MAX_QUEUE_CONNECTIONS"])

    while True:
        # Wait for a connection
        print('waiting for a connection...')
        connection, client_address = sock.accept()

        query = "[]"
        while True:
            try:
                query = connection.recv(settings["BUFFERSIZE"]).decode("utf8").rstrip()
                cursor.execute(query)
                db_conn.commit()

                reply = []
                if cursor.description is not None:
                    columns = [i[0] for i in cursor.description]
                    for row in cursor.fetchall():
                        dict = {}
                        for n, col in enumerate(row):
                            dict[columns[n]] = col
                        reply.append(dict)

                reply = json.dumps(reply)
                connection.sendall(bytes("\n" + reply + "\n", "utf-8"))
                print(f"Reply:{reply}")

            except (sqlite3.OperationalError, UnicodeDecodeError, sqlite3.IntegrityError) as e:
                print(f"Error processing query {query}")
                print(str(e))
                reply = json.dumps({"error": str(e)})
                connection.send(bytes(len(reply)))

                connection.send(bytes(reply + "\n", "utf-8"))

            except BrokenPipeError:
                print("connection closed")
                connection.close()
                break

if __name__ == "__main__":
    main()
