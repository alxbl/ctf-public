#!/bin/sh

REMOTE=local

# Creating files/chal2/cfg/backup,key
# dd if=/dev/urandom bs=32 count=1 | tee cfg/backup.key | xxd 

# Create containers if not present.
CONTAINER=$(lxc list "$REMOTE:ctn-abeaulieu-.*" --format json)
if [ "$CONTAINER" == "[]" ]; then
  echo "Containers not found... creating them."
  
  # Local setup. See README.md.
  lxc launch images:ubuntu/focal/cloud $REMOTE:ctn-abeaulieu-avatarsvc-1 --vm -c security.secureboot=false
  lxc launch images:ubuntu/focal $REMOTE:ctn-abeaulieu-avatarsvc-2 -c security.privileged=true                                                                                                          master 102d ● ⬡
fi

# Setup containers
ANSIBLE_PIPELINING=True ansible-playbook -i hosts playbook.yml $@