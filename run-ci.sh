#!/bin/bash
trap "docker-compose -f docker-compose.ci.yml down --rmi local"  EXIT
echo "Spin 'ci_container' up"
docker-compose \
    -f docker-compose.ci.yml \
    up -d
cmd="docker exec -t -w /work ci_container pwsh ./Make.ps1 -command Test"
echo $cmd
eval $cmd
exit 0