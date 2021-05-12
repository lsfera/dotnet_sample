# Jazzing up legacy stuff

Solution protype: just a couple of .net5.0 console apps:

* DataGenerator simulates your very lucrative upstream system that feeds RabbitMQ broker with a huge amount of PurchaseOrder messages.
* Daemon provides message consuming capbabilities with at-least-once semantics: within its UOW data are atomically persisted to Cassandra host. A pretty common scenario indeed.

For my fellow diagram lovers..

![architecure](diagram.svg)
  
Used LCOW ( linux container on windows )  to keep the a minimal footprint on host resources.  
Minimal requirements(for a Windows OS machine):

* .[net5.0 sdk](https://dotnet.microsoft.com/download/dotnet/5.0)
* [Docker for windows](https://desktop.docker.com/win/stable/Docker%20Desktop%20Installer.exe) wsl2 mode

## Demo mode

### TL;DR

```pwsh
&.\run-demo.ps1
```

### Explained version

1. Prepare environment:

```
docker network create demo
docker-compose `
  -f .\rabbitmq.docker-compose.yml `
  -f .\cassandra.docker-compose.yml `
  up -d
```

This will enable:

* a RabbitMQ container [rabbitmq]:
  * with admin plugin available at <http://localhost:15674/Fulfillment> with default credentials( guest:guest )
  * listening on [rabbitmq:5672] for tcp connections
* a Cassandra container on [cassandra]
  * listening on default port - 9042
  * with default credentials ( cassandra:cassandra )
  * with the defined [fulfillment](./docker/cassandra/configure-db.cql) keyspace

It can take a while for cassandra initialization complete...

2. Run consuming application ( scaled to 2 containers to provide competing consuming scenario):

* a few locs consumer
  * bound to daemon:purchase-order queue
  * with [dlx](https://www.rabbitmq.com/dlx.html) enabled

```pwsh
start powershell {
    docker-compose -f .\daemon.docker-compose.yml  --compatibility up --scale daemon=2
}
```

3. Run data generator

```pwsh
start powershell {
    docker-compose -f .\datagenerator.docker-compose.yml up
}
```

### Sum up

4. View our data on cassandra

```
docker exec cassandra cqlsh -e "select json * from fulfillment.orders;"
```

### Resource clean up

```
docker-compose `
    -f .\rabbitmq.docker-compose.yml `
    -f .\cassandra.docker-compose.yml `
    -f .\daemon.docker-compose.yml `
    -f .\datagenerator.docker-compose.yml `
    down
docker network demo rm
```

## Integration tests - in isolation

Well...
Definitely this was my main goal since the beginning, hence infrastructure dependencies are spun up(setup)/down(teardown) for every test cycle - thanks to [FluentDocker](https://github.com/mariotoffia/FluentDocker) creator for such an OSS contribution.  
Integration tests run within the [CI context](##CI) and locally - but with a different approach.

* Locally - through looback ip binding:
  * Debug from within VS ( 'Local' configuration)
  * Run from cli using the following snippet:

```
dotnet test Demo.sln -c Local
```

## CI

Within a conteinerized contex you have to use the 'Debug' configuration that refers to hostnames - resolved within the network boundaries.  
For major convenience I provided a docker-out-of-docker environment where you can play the full CI flow.

```bash
run-ci.sh
```

Refer to [Make.ps1](./Make.ps1) for details on tasks and available options.  
Worth to notice, we're creating windows artifacts from a linux box :)