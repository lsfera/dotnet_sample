#!/bin/bash
trap "docker-compose -f docker-compose.ci.yml down --remove-orphans --rmi all"  EXIT
echo "Spin 'ci_container' up"
docker-compose \
    -f docker-compose.ci.yml \
    up -d
declare -a cmds=( \
"docker exec -t ci_container pwsh ./Make.ps1 -command Nuke" \
"docker exec -t ci_container pwsh ./Make.ps1 -command Test" \
"docker exec -t ci_container pwsh ./Make.ps1 -command Build" \
"docker exec -t ci_container pwsh ./Make.ps1 -command Pack"
)
for cmd in "${cmds[@]}"
do
  echo $cmd
  eval $cmd
done
exit 0