Write-Host "Setup infrastructure"
$env:BASE_PATH="."
$network="demo"
Write-Host "Create network $network"
docker network create $network
Write-Host "Create containers"
docker-compose `
    -f .\rabbitmq.docker-compose.yml `
    -f .\cassandra.docker-compose.yml `
    -f .\daemon.docker-compose.yml `
    -f .\datagenerator.docker-compose.yml `
    down
Write-Host "Waiting for container readiness"
start powershell {
                    docker-compose `
                        -f .\rabbitmq.docker-compose.yml `
                        -f .\cassandra.docker-compose.yml `
                    up
                    pause
                }
do
{
    Start-Sleep -s 5
    $state=$(docker inspect cassandra-sidecar|ConvertFrom-Json).State
    $status=$state.Status
    $exitCode=$state.ExitCode
    $restart=$state.Restarting
}Until(($status -eq "exited") -and ($exitCode -eq 0) -and ($restart -eq $false))
Write-Host "Running consumer"
start powershell { 
    docker-compose -f .\daemon.docker-compose.yml  --compatibility up --scale daemon=2
    pause
}
Read-Host -Prompt "Enter to run publisher - wait for consumer to start"
echo "http://localhost:15674/#/queues for RabbitMQ management"
start powershell {
    $env:MESSAGES=100; 
    docker-compose -f .\datagenerator.docker-compose.yml up
    pause
}
Read-Host -Prompt "Enter to verify data on cassandra"
$queries = @( `
"select json * from fulfillment.orders limit 100;" `
)
foreach ($query in $queries) {
   echo $query
   docker exec -t cassandra cqlsh -e $query
}


Read-Host -Prompt "Enter to tear infrastructure down"
Write-Host "TearDown containers"
docker-compose `
    -f .\rabbitmq.docker-compose.yml `
    -f .\cassandra.docker-compose.yml `
    -f .\daemon.docker-compose.yml `
    -f .\datagenerator.docker-compose.yml `
    down `
    --rmi local
Write-Host "TearDown network $network"
docker network rm $network