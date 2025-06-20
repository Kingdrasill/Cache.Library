# Implementação de Cache como Biblioteca em C#

## 📑 Sumário

- [📖 Visão Geral](#-visão-geral)
- [🏗️ Arquitetura da Biblioteca](#️-arquitetura-da-biblioteca)
- [📦 Namespaces e Classes](#-namespaces-e-classes)
  - [📂 Cache.Library](#-cachelibrary)
  - [📂 Cache.Test](#-cachetest)
- [⚙️ Configuração](#️⚙-configuração)
- [🌱 Núcleo](#-núcleo)
  - [CacheItem](#cacheitem)
  - [CacheTable](#cachetable)
  - [Eviction Policy](#eviction-policy)
  - [Adjuster Policy](#adjuster-policy)
  - [Cache Provider](#cache-provider)
  - [Cache Manager](#cache-manager)
- [📊 Diagnósticos](#-diagnósticos)
  - [CacheMetrics](#cachemetrics)
- [🗑️ Políticas de Remoção (Evicition Policies)](#️🗑-políticas-de-remoção-evicition-policies)
  - [LFRU](#lfru)
  - [No Eviction](#no-eviction)
- [🪛 Políticas de Ajustador (Adjuster Policies)](#-políticas-de-ajustador-adjuster-policies)
  - [Default](#default)
- [🗃️ Provedores de Cache (Cache Providers)](#️-provedores-de-cache-cache-providers)
  - [Default](#default-1)
- [🌐 Gerenciador da Cache (Cache Manager)](#-gerenciador-da-cache-cache-manager)
  - [Estruturas Internas](#estruturas-internas)
  - [Funções Principais](#funções-principais)
    - [AddItem](#additemkey-identifier-data-hourstolive-expirable-keepcached-fill-out-message)
    - [GetItem](#getitemkey-identifier-out-message)
    - [GetItems](#getitemskey-out-message)
    - [GetValues](#getvalues)
    - [SetItemStale](#setitemstalekey)
    - [SetCapacity](#setcapacitycapacity-forced)
    - [EvictStaleOrExpired](#evictstaleorexpired)
    - [AdjustValues](#adjustvalues)
    - [LogMetrics](#logmetrics)
- [🧪 Testes Unitários](#-testes-unitários)
- [📚 Uso da Biblioteca de Cache em uma API](#-uso-da-biblioteca-de-cache-em-uma-api)
  - [Armazenamento de Informações de Cache](#armazenamento-de-informações-de-cache)
  - [Configuração via Injeção de Dependência](#configuração-via-injeção-de-dependência)
  - [Utilização da Cache na API](#utilização-da-cache-na-api)
    - [Commands](#commands)
    - [Queries](#queries)
    - [Serviços Auxiliares de Cache](#serviços-auxiliares-de-cache)
- [📏 Medindo a Performance da Cache](#📏-medindo-a-performance-da-cache)
  - [Visão Geral do Desempenho](#visão-geral-do-desempenho)
  - [Cenário de Teste](#cenário-de-teste)
    - [Fluxo dos Testes](#fluxo-dos-testes)
  - [Resultados Obtidos](#resultados-obtidos)
    - [Uso de Memória](#uso-de-memória)
    - [Uso de CPU (%)](#uso-de-cpu-)
    - [Quantidade de Queries](#quantidade-de-queries)
    - [Tempo de Execução em Média (ms)](#tempo-de-execução-em-média-ms)

## 📖 Visão Geral

`CacheLibrary` é uma biblioteca de classes para .NET desenvolvida sem uso de de bibliotecas de cache externas. Ela foi feita para aprender como funciona um sistema de cache na prática e para ser utilizada em outras aplicações como um sistema de cache em memória simples.

## 🏗️ Arquitetura da Biblioteca

```
Cache.Library.Solution
    |-->Cache.Library
    |       |--> Configuration
    |       |--> Core
    |       |       |--> Models
    |       |--> Diagnostics
    |       |--> Management
    |       |--> Policies
    |       |--> PolicyAdjusters
    |       |--> Providers
    |-> Cache.Test   
```

## 📦 Namespaces e Classes

### 📂 Cache.Library
- 📂 **AdjusterPolicies**
  - 📄 `DefaultAdjusterPolicy.cs`: Ajustador padrão de política.
- 📂 **Configuration**
  - 📄 `CacheOptions.cs`: Define as opções configuráveis da cache.
- 📂 **Core**
  - 📂 **Models**
    - 📄 `CacheItem.cs`: Modelo de um item da cache.
    - 📄 `CacheTable.cs`: Modelo da estrutura pricipal de armazenamento
  - 📄 `ICache.cs`: Interface para implementação de cache
  - 📄 `ICachePolicyAdjuster.cs`: Interface para política de adjuste.
  - 📄 `ICacheProvider.cs`: Interface para proverdor de cache.
  - 📄 `IEvictionPolicy.cs`: Interface para política de remoção.
- 📂 **Diagnostics**
  - 📄 `CacheMetrics.cs`: Coleta e expõe métrics de uso da cache.
- 📂 **Management**
  - 📄 `CacheManager.cs`: Controlador de alto nível para operações na cache.
- 📂 **Policies**
  - 📄 `LfruEvictionPolicy.cs`: Política LFRU (Least Frequently Recently Used).
  - 📄 `NoEvictionPolicy.cs`: Política sem remoção.
- 📂 **Providers**
  - 📄 `DefaultCacheProvider.cs`: Implementação básica de cache provider.
### 📂 Cache.Test
- 📄 `LfruEvictionTest.cs`: Testes da política LFRU.
- 📄 `NoEvictionTest.cs`: Testes da política sem remoção.

## ⚙️ Configuração

A classe `CacheOptions` representa a estrutura de configuração da cache. Ela define os parâmetros iniciais que controlam o comportamento da cache no momento de sua criação ou inicialização.

Essa classe centraliza três configurações principais:

- **Capacity** - determina a capacidade máxima da cache em bytes. Por padrão, o valor inicial é 128 * 1024 * 1024 (128 MiB).
- **EvictionPolicy** - define o nome da política de remoção (eviction) que será utilizada pela cache para decidir quais itens serão descartados quando houver necessidade de liberar espaço. O valor padrão é "lfru".
- **PolicyAdjuster** - especifica o nome da política de ajuste de valores (HoursToLive, Expirable e KeepCached) que será aplicada aos itens da cache. O valor padrão é "default".

Essa classe possui apenas um método:

- **SetCapacity** - permite alterar dinamicamente a capacidade máxima da cache.

Essa estrutura funciona como ponto de configuração central antes da instânciação do gerenciador de cache, permitindo personalizar o comportamento padrão sem alterar a implementação das interfaces ou da lógica de negócio.

## 🌱 Núcleo

O `Core` concentra as estruturas e contratos fundamentais do sistema de cache. Ele define as interfaces, classes base e entidades responsáveis pelo funcionamento interno da cache, sem dependências de implementação ou integração externa.

### CacheItem

Representa o item que está sendo armazenado na cache, contendo as seguintes informações:

- **Key** - a chave que identifica o item na cache.
- **Values** - um dicionário de dicionários, onde o dicionário principal identifica cada grupo de valores por uma chave específica, e cada dicionário interno armazena os valores de um objeto.
- **HoursToLive e LastUsed** - utilizados na política de invalidação. Se a data em LastUsed somada ao valor em HoursToLive for menor que a data atual no momento da consulta, o item é considerado inválido.
- **Expirable** - variável auxiliar que indica se o item será ou não invalidado com o tempo.
- **KeepCached** - variável que determina se o item pode ser removido da cache no momento da substituição. Um item com KeepCached permanece na cache caso o item candidato à substituição não possua essa marcação.
- **Stale** - variável que indica se o item está obsoleto.
- **EstimatedSize** - valor estimado do tamanho do item na cache.

### CacheTable

Representa a estrutura principal da cache, contendo as seguintes informações:

- **Cache** - um dicionário de `CacheItem`, onde cada item é identificado por sua chave. Essa estrutura armazena os dados da cache.
- **Capacity** - valor que define a capacidade máxima da cache.
- **UsedSize** - valor que representa o espaço atualmente utilizado na cache, calculado pela soma dos `EstimatedSize` de todos os `CacheItem` armazenados.
  
Além dessas informações, há alguns métodos responsáveis por manipular os dados da cache:

- **Includes** -  função com duas implementações distintas, mas de propósito semelhante: verifica se uma chave de `CacheItem` existe na cache, ou se uma chave de `CacheItem` e uma chave de item específico dentro de um `CacheItem` existem.
- **ExpiredOrStale** - função que verifica se um determinado item na cache está expirado ou obsoleto, ou seja, se está inválido para uso.
- **TryGet** -  função que retorna os dados de um `CacheItem` presente na cache.
- **TryGetItem** - função que retorna um item específico armazenado dentro de um `CacheItem`.
- **AddOrUpdate** -  função para adicionar um novo `CacheItem` ou atualizar um existente na cache. Antes de adicionar ou atualizar, verifica se o item cabe dentro da capacidade disponível. No caso de atualização, o `CacheItem` anterior é removido completamente, e o novo é adicionado em seu lugar. A capacidade utilizada da cache é atualizada, e o item recém-inserido é marcado como usado.
- **SetCapacity** - função para definir uma nova capacidade máxima da cache. A alteração só é permitida se a nova capacidade for maior ou igual ao espaço atualmente em uso.
- **SetItemStale** - função para marcar um item da cache como obsoleto.
- **SetItemExpirability** - função para mudar o valor `Expirable` de um item da cache.
- **SetItemKeepCached** - função para mudar o valor `KeepCached` de um item da cache.
- **EvictStaleOrExpired** - função que remove da cache todos os itens que estão inválidos (expirados ou obsoletos) e, ao final, retorna todos as chaves de todos os itens removidos.
- **Destroy** - função para remover manualmente um item específico da cache.
- **AdjustValues** - função que atualiza os valores de `HoursToLive`, e `Expirable` de um determinado item da cache.

### Eviction Policy

Representa uma estratégia que define qual item será removido da cache quando necessário. Para adicionar uma nova política de remoção, é necessário implementar a interface `IEvictionPolicy`. Essa interface exige a implementação dos seguintes métodos:

- **OnItemAcessed** - função que define como deve ser tratado um item que foi acessado.
- **OnitemAdded** - função que define como deve ser tratado um item que foi adicionado à cache.
- **OnitemRemoved** - função que define como deve ser tratado um item que foi removido da cache.
- **SelectItemToEvict** - função responsável por decidir qual item será removido da cache. Recebe o dicionário de `CacheItem` da cache e um valor auxiliar que informa se pode remover itens marcados com `KeepCached`, se necessário. Ao final, retorna a chave do item a ser removido ou `null` caso não haja item elegível.
- **GetFreq** - função que retorna a frequência de uso de um item na cache.

### Adjuster Policy

Representa uma estratégia que define como os valores de `HoursToLive` e `Expirable` de um `CacheItem` devem ser ajustados. Para adicionar uma nova política de ajuste, é necessário implementar a interface `ICacheItemPolicyAdjuster`. Essa interface exige a implementação dos seguinte método:

- **Adjust** - recebe a frequência de uso de um item da cache e, a partir desse valor, retorna um dicionário contendo os novos valores de `HoursToLive` e `Expirable` para o item em questão.

### Cache Provider

Representa  a estrutura responsável por armazenar e gerenciar os itens na memória. Para implementar um provedor de cache deve ser implementado `ICacheProvider`, logo é necessário definir os seguintes membros:

- **Includes** - função com duas implementações distintas, mas de propósito semelhante: se existe um `CacheItem` com a chave especificada ou se existe um `CacheItem` com a chave especificada e se dentro dele existe um item com o identificador informado.
- **ExpiredOrStale** - verifica se o item da cache está expirado ou obsoleto.
- **Get** - recupera um CacheItem da cache a partir pela sua chave.
- **GetItem** - recupera um item específico dentro de um CacheItem a partir da sua chave e do seu identificador.
- **AddItem** - adiciona um novo CacheItem na cache. Retorna se foi possível adicionar o item.
- **RemoveItem** - remove um item da cache pela sua chave.
- **AdjustValues** - ajusta os valores de HoursToLive, Expirable e KeepCached de um item específico.
- **SetItemStale** - marca um item da cache como obsoleto. 
- **SetItemExpirability** - muda o valor `Expirable` de um item da cache.
- **SetItemKeepCached** - muda o valor `KeepCached` de um item da cache.
- **EvictStaleOrExpired** - remove todos os itens expirados ou obsoletos da cache e retorna uma lista com as chaves dos itens removidos.
- **SetCapacity** - define uma nova capacidade máxima para a cache. Retorna `false` se a nova capacidade for menor que o tamanho atualmente em uso.

### Cache Manager

Representa o contrato para o serviço de gerenciamento da cache na aplicação, responsável por controlar o armazenamento e acesso dos itens, além de expor operações de manutenção. Para criar um novo gerencidaor de cache deve ser implementada interface `ICache`, em que devem ser implementados os métodos:

- **AddItem** - adiciona um novo item na cache, recebe junto uma variável para verificar se é uma adição de encher a cache ou não.
- **GetItems** - recupera todos os itens associados a um CacheItem pela chave.
- **GetItem** - recupera um item específico de dentro de um CacheItem, pela chave e identificador.
- **GetValues** - retorna todos os dados auxiliares, como dados da politíca de remoção ou de invalidação, de todos os itens armazenados na cache em formato de lista.
- **SetItemStale** - marca um item da cache como obsoleto.
- **SetItemExpirability** - muda o valor `Expirable` de um item da cache.
- **SetItemKeepCached** - muda o valor `KeepCached` de um item da cache.
- **SetCapacity** - ajusta a capacidade máxima da cache. Utiliza um variável para forçar ou não a alteração mesmo se a nova capacidade for menor que a atual.
- **EvictStaleOrExpired** - remove todos os itens que estão expirados ou obsoletos.
- **AdjustValues** - ajusta os valores de HoursToLive, Expirable e KeepCached de acordo com a estratégia configurada no Policy Adjuster de todos os items da cache.
- **LogMetrics** - registra as métricas atuais da cache (uso de memória, quantidade de itens, capacidade utilizada etc.).

## 📊 Diagnósticos

O módulo de diagnósticos permite acompanhar informações sobre o uso atual da cache em tempo de execução. Ele facilita a visualização da capacidade configurada, do espaço ocupado e do estado dos itens armazenados, sendo útil para análises de desempenho e para identificar possíveis gargalos no gerenciamento de memória.

### CacheMetrics

A classe `CacheMetrics` centraliza métodos de diagnóstico e logging das informações da cache diretamente no console. Ela não armazena métricas historicamente, mas fornece uma forma prática de inspecionar o estado atual.

Essa possui dois métodos:

- **LogCapacity** - exibe no console a capacidade máxima configurada para a cache.
- **LogUsedCache** - mostra no console:
    - O espaço usado pela cache em MB comparado com a capacidade máxima.
    - A lista de itens armazenados, exibindo para cada um:
      - Chave do item
      - Frequência de acesso (quantas vezes foi acessado)
      - Tempo de vida configurado (HoursToLive)
      - Se o item é expirável ou não (Expirable)
      - Se o item está marcado para persistir na cache (KeepCached)

## 🗑️ Políticas de Remoção (Evicition Policies)

A política de remoção (eviction policy) é responsável por definir a estratégia de descarte de itens armazenados na cache, quando há necessidade de liberar espaço ou quando determinados critérios de validade são atingidos.

No sistema, duas políticas de remoção estão implementadas:

### LFRU

A `LfruEvictionPolicy` implementa uma política híbrida baseada em dois critérios:

- **LFU (Least Frequently Used)** - remove itens menos acessados.
- **LRU (Least Recently Used)** - em caso de empate na frequência de uso, remove o item menos recentemente acessado.

Essa classe possui duas informações:

- **AccessOrder** - fila encadeada que guarda a ordem de acesso das chaves da cache.
- **FrequencyCounter** - dicionário que guarda a quantidade de vezes que cada chave da cache foi usada.

Ela fuciona da seguinte maneria:

- Cada item armazenado na cache tem um contador de acessos.
- Toda vez que um item é acessado, seu contador é incrementado e ele é movido para o final de uma lista de acesso.
- Quando é necessário remover um item:
    1. Primeiro tenta-se remover os itens marcados como stale (obsoletos), priorizando o de maior tamanho.
    2. Se não houver stale, remove o item sem `KeepCached` com menor frequência de uso.
    3. Em caso de empate na frequência, remove o item mais antigo entre eles.
    4. Se todos tiverem `KeepCached`, e a remoção for forçada, aplica o mesmo critério ignorando o `KeepCached`.

### No Eviction

A `NoEvictionPolicy` é uma política especial onde nenhum item é removido automaticamente da cache.

Ela fuciona da seguinte maneria:

- Não faz nada ao acessar, adicionar ou remover itens da cache.
- Se for preciso liberar espaço e a remoção for forçada, remove o primeiro item encontrado no dicionário da cache.
  - Caso contrário, se nenhuma remoção for permitida, retorna uma mensagem informando que nenhum item pode ser removido.

## 🪛 Políticas de Ajustador (Adjuster Policies)

As políticas de ajustador definem estratégias para ajustar dinamicamente os parâmetros de um item armazenado na cache, com base na sua frequência de uso. Esses parâmetros são:

- `HoursToLive` (tempo de validade do item em horas)
- `Expirable` (se o item pode expirar automaticamente)
- `KeepCached` (se o item deve ser persistir na cache)

Atualmente, o sistema conta com a seguinte política implementada:

### Default

A `DefaultAdjusterPolicy` é a política padrão de ajuste de itens na cache. Ela altera os parâmetros de acordo com a frequência de acesso do item, usando regras simples:

Ela fuciona da seguinte maneria:

- Se o item tiver sido acessado 10 vezes ou menos:
  - HoursToLive = 1 hora
  - Expirable = true
- Se o item tiver sido acessado entre 11 e 50 vezes:
  - HoursToLive = frequency / 10 horas (proporcional à frequência)
  - Expirable = true
- Se o item tiver sido acessado entre 51 e 99 vezes:
  - HoursToLive = 10 horas
  - Expirable = true
- Se o item tiver sido acessado 100 vezes ou mais:
  - HoursToLive = 20 horas
  - Expirable = false

## 🗃️ Provedores de Cache (Cache Providers)

Um provedor de cache é responsável por executar as operações básicas de armazenamento, recuperação, remoção e gerenciamento de itens na cache. Ele funciona como um serviço intermediário entre o gerenciado e a estrutura interna de dados que armazena os itens em cache.

### Default

A classe `DefaultCacheProvider` é a implementação padrão da interface `ICacheProvider` e utiliza internamente uma estrutura chamada CacheTable para manter os itens em cache. Ela expõe métodos para manipular os dados e acessar propriedades relacionadas ao estado atual da cache.

Essa classe possui uma estrutura de dados que é a `Cache` que é uma instância de `CacheTable` que contém os itens efetivamente armazenados e a lógica de manipulação. Além de implementar os métodos de `ICacheProvider` usando os métodos de `Cache`. 


## 🌐 Gerenciador da Cache (Cache Manager)

A classe `CacheManager` é a principal responsável por controlar o ciclo de vida dos itens na cache, realizando operações como adicionar, recuperar, remover, ajustar e diagnosticar os dados armazenados. Ela implementa a interface `ICache`.

### Estruturas Internas

- **Provider** - gerencia o armazenamento físico e a leitura/escrita dos itens na cache, uma instância de `ICacheProvider`.
- **Policy** - define a política para remoção de itens quando for necessário liberar espaço, uma instância de `IEvictionPolicy`.
- **Adjuster** - permite ajustar valores complementares dos itens da cache, com base na frequência de uso ou outros critérios, uma instância de `ICacheItemAdjusterPolicy`.
- **Metrics** - realiza o diagnóstico e logging do estado da cache, uma instância de `CacheMetrics`.
- **Options** - guarda as configurações ativas da cache, como capacidade e políticas, uma instância de `CacheOptions`

### Funções Principais

As principais funções desta classe são:

#### AddItem(key, identifier, data, HoursToLive, expirable, keepCached, fill, out message)

Adiciona um novo `CacheItem` com os dados de `data` à cache.

- **Recebe**: chave, identificador, dados, configurações de expiração, e flags de comportamento.
- **Valida os dados**: garante que todos os dicionários da lista de dados contenham o identificador.
- **Cria um** `CacheItem` com os dados e metadados.
- **Tenta adicionar na cache** usando o provider.
- Se não houver espaço suficiente:
  - Se a flag **fill** for **false**, aplica a política de remoção para liberar espaço até conseguir.
  - Se não conseguir adicionar mesmo após remoções, retorna um erro e uma mensagem sobre o erro.
- **Registra o item na política de remoção**, atualizando os dado de controle dela.

#### GetItem(key, identifier, out message)

Recupera um item específico de um `CacheItem` da cache.

- **Recebe**: chave e identificador.
- **Verifica se o item existe**.
- **Verifica se está expirado ou obsoleto**.
- Se existir, **marca o item como acessado na política de remoção**.
- Retorna o item ou uma mensagem de erro.

#### GetItems(key, out message)

Recupera todos os valores de um `CacheItem` da cache pela chave.

- **Recebe**: chave.
- **Verifica se o item existe**.
- **Valida se o item está expirado ou obsoleto**.
- Se existir, **marca como acessado na política**.
- Retorna todos os valores do item ou uma mensagem de erro.

#### GetValues()

Retorna informações complementares de todos os itens da cache.

- Percorre a cache e gera uma lista com os dados de cada item da cache:
  - Chave
  - Frequência de acesso
  - Tempo de vida
  - Flags de expiração e persistência na cache

#### SetItemStale(key)

Marca manualmente um item como obsoleto (stale) por sua chave.

- Verifica se o item existe na cache.
- Se sim, marca ele como obsolte.

#### SetCapacity(capacity, forced)

Altera a capacidade máxima da cache.

- **Recebe**: capacidade e uma flag de comportamento.
- Se `forced` for `true`: remove itens usando a política de remoção até caber na nova capacidade.
- Se não for possível ajustar e `forced` for `false`, mantém a capacidade atual.
- Se for possível mudar a capacidade, atualiza a configuração interna e registra a nova capacidade no log de métricas.

#### EvictStaleOrExpired()

Remove automaticamente todos os itens expirados ou marcados como obsoletos.

- Percorre a cache e remove os itens.
- Atualiza a política de remoção sobre os itens eliminados.

#### AdjustValues()

Permite ajustar dinamicamente os valores armazenados com base na frequência de acesso.

- Para cada item da cache:
  - Obtém a frequência atual.
  - Calcula novos valores via a política de ajuste.
  - Atualiza os dados do item usando o provider.

#### LogMetrics()

Gera um diagnóstico de uso atual da cache.

- Percorre a cache coletando:
  - Frequência de acesso de cada item.
  - Dados de controle de cada item.
- Usa `Metrics` para registrar no console:
  - Capacidade utilizada.
  - Capacidade total.
  - Listagem de itens e seus detalhes.

## 🧪 Testes Unitários  

Para garantir a integridade e o correto funcionamento da solução de cache implementada, foram desenvolvidos testes unitários utilizando o framework **xUnit**. Os testes validam o comportamento da classe `CacheManager` sob diferentes configurações de políticas de descarte (eviction) e situações de uso.

Os testes foram organizados em duas classes principais:

- `LfruEvictionTest`: valida operações de cache utilizando a política LFRU (Least Frequently Recently Used).
- `NoEvictionTest`: valida operações de cache sem política de descarte definida.

Cada classe possui um método auxiliar `CreateManager()` responsável por instanciar o `CacheManager` com as opções de configuração necessárias para cada cenário.

Em ambas as classes de testes, foram contemplados os seguintes cenários:

| Cénario | Descrição |
|---------|-----------|
| Adicionar e recuperar item | Verifica se um item adicionado à cache pode ser recuperado corretamente. |
| Falha ao adicionar item se exceder a capacidade | Garante que a cache rejeita itens cujo tamanho ultrapasse sua capacidade máxima configurada. |
| Marcar item como obsoleto (stale) | Verifica se um item pode ser marcado como obsoleto e se, após isso, ele deixa de ser acessível. |
| Buscar item expirado | Testa a remoção lógica de itens que atingiram seu tempo de validade, retornando mensagem apropriada. |
| Buscar item obsoleto | Valida que itens marcados como obsoletos não podem mais ser recuperados. |
| Ajustar valores internos da cache | Garante a execução do método `AdjustValues()`, que normaliza ou ajusta valores internos da cache. |
| Retornar valores complementares armazenados na cache | Verifica se os dados complementares armazenados podem ser listados corretamente via o método `GetValues()`. |

Esses testes unitários garantem:

- Confiabilidade das operações básicas da cache.
- Comportamento esperado em situações limite, como estouro de capacidade e expiração de itens.
- Adequação das políticas de descarte conforme configurado.
- Cobertura de erros e mensagens informativas adequadas ao usuário/serviço consumidor da cache.

## 📚 Uso da Biblioteca de Cache em uma API

Este tópico descreve como integrar e utilizar a biblioteca de cache desenvolvida dentro de uma API, incluindo a configuração via injeção de dependência, utilização nos commands e queries, e serviços auxiliares para manutenção e antecipação de cache.

### Armazenamento de Informações de Cache

Para controlar e organizar o uso da cache, foi criada uma tabela adicional no banco de dados da aplicação. Essa tabela armazena as informações de controle necessárias para a operação da cache, como:

- Nome da tabela ou entidade
- Frequência de uso
- Tempo de vida (TTL)
- Status (ativo ou obsoleto)

### Configuração via Injeção de Dependência

A cache foi configurada globalmente na API utilizando injeção de dependência. O exemplo abaixo demonstra como configurar o `CacheManager` e registrar a interface `ICache`:

```
services.Configure<CacheOptions>(configuration.GetSection("CacheOptions"));
services.AddSingleton<ICache>(sp => {
    var options = sp.GetRequiredService<IOptions<CacheOptions>>().Value;
    return new CacheManager(options);
});
```

### Utilização da Cache na API

A cache é utilizada em dois pontos principais: nos *commands* (operações de gravação) e *queries* (operações de leitura) das entidades.

Essa tabela permite que a API saiba quais informações podem ser armazenadas em cache e realize a gestão adequada desses dados.

#### Commands

Nos *commands*, toda vez que um item é criado, atualizado ou removido no banco, a respectiva entrada na cache é marcada como obsoleta:

```
if (_useCache.Use)
{
    if (_cache.SetItemStale(nameof(Category)))
        Console.WriteLine("Item marcado como obsoleto!");
    else
        Console.WriteLine("item não está na cache!");
}
```

#### Queries

Nas *queries*, ao requisitar um item:

- Verifica se o item está disponível na cache.
- Se estiver, retorna diretamente.
- Caso contrário, busca no banco e armazena o item na cache antes de retornar.

```
if (_useCache.Use)
{
    var cacheResponse = GetFromCache(id);
    if (cacheResponse.IsSuccess)
        return cacheResponse;
}

var repoResponse = await GetFromRepository(id, NoTracking);
if (repoResponse.IsFailure)
    return repoResponse;

if (_useCache.Use)
    await SendToCache();

return repoResponse;
```

#### Serviços Auxiliares de Cache

Além do uso nos *commands* e *queries*, foram implementados dois serviços adicionais para o gerenciamento automático da cache:

| Serviço | Descrição | Intervalo |
|---------|-----------|-----------|
| `CacheCleanupService` | 	Responsável por limpar itens obsoletos ou expirados da cache. | A cada 1 min |
| `CacheAheadService` | Realiza **cache-ahead**, antecipando dados mais requisitados, e atualiza a tabela de controle no banco. | A cada 10 min |

O **cache-ahead** é executado uma única vez durante a inicialização da aplicação, preenchendo a cache com os dados mais acessados. Após isso, o serviço segue apenas atualizando a tabela de controle no banco periodicamente.

**Lógica de Execução do Cache-Ahead**

1. Consulta a tabela de controle no banco para obter informações dos dados elegíveis para cache.
2. Ordena os dados por frequência de uso (do maior para o menor).
3. Para cada item (até a cache atingir sua capacidade):
     - Busca o identificador e informações completas no banco.
     - Tenta adicionar na cache.
     - Caso não haja espaço suficiente, o item é descartado.

## 📏 Medindo a Performance da Cache

### Visão Geral do Desempenho

Esta seção apresenta os resultados de testes realizados para avaliar o impacto da utilização da biblioteca `CacheLibrary` na performance de uma API ASP.NET Core. Os testes envolveram medições de memória, CPU, tempo de execução e número de consultas ao banco de dados, com e sem o uso da cache.

### Cenário de Teste

Para testar o impacto da cache, foi realizada a execução da API de e-commerce `Cartsys.Teste` com e sem cache, a fim de verificar os ganhos e/ou perdas de performance.
No caso da API utilizando cache, foram feitos dois cenários:

- Cenário 1: A cache já continha os dados necessários para os testes (utilizando os serviços de cache ahead e cache cleanup).
- Cenário 2: A cache não possuía os dados ou continha dados inválidos.

A cache utilizada nos testes foi configurada com:

- 256 MiB de capacidade
- Política de remoção LFRU
- Política de ajuste de dados padrão (default)

#### Fluxo dos Testes

O fluxo executado em cada cenário foi o seguinte:

1. Criar um usuário.
2. Realizar a autenticação com o usuário criado.
3. Buscar os dados do usuário.
4. Atualizar os dados do usuário.
5. Capturar snapshot de memória.
6. Listar todos os produtos.
7. Selecionar um produto.
8. Criar um pedido.
9. Selecionar o pedido criado.

### Resultados Obtidos

#### Uso de Memória

| Métrica | Sem Cache | Com Cache |
|---------|-----------|-----------|
| Memória de Processo | 111 MB | 122 MB | 
| GC Heap Size | 36,67 MiB | 42,49 MiB |
| Working Set | 209,57 MiB | 225,94 MiB |

Com o uso da cache, houve um aumento de aproximadamente 9,90% no consumo de memória de processo.
O tamanho do *heap* do *Garbage Collector* também cresceu cerca de 15,87%, além de um aumento no *working set* de aproximadamente 7,81%.

#### Uso de CPU (%)

| Função | Sem Cache | Com Cache |
|--------|-----------|-----------|
| Kernel | 49,3% | 45,5% | 
| Banco de Dados | 32,2% | 36,6% |
| ASP.NET | 9,9% | 7,9% |
| Json | 4,2% | 5,1% |
| Registro em log | 2,5% | 2,3% | 

Em relação ao uso da CPU, não houve um impacto significativo com a ativação da cache, variando o aumento ou diminuição do uso da CPU entre 0 a 5% nas operações.

#### Quantidade de Queries

| | Sem Cache | Com Cache |
|-|-------------|-------------|
| Total de Queries | 16 | 32 |

Devido à forma como a cache foi testada - utilizando cache-ahead e realizando invalidações apenas em operações de inserção, atualização ou exclusão - houve um aumento no número de queries realizadas no banco de dados.

#### Tempo de Execução em Média (ms)

**Buscar usuário por Id - Banco possui o Id**

| | **1ª Chamada** | **Chamadas Posteriores** |
|-|----------------|--------------------------|
| Sem Cache | 1764ms | 7-92ms |
| Com Cache | 2ms | 0ms |
| Com Cache - Tabela Usuários Inválida | 237ms | 21-45ms |

**Buscar usuário por Id - Banco não possui o Id**

| | **1ª Chamada** | **Chamadas Posteriores** |
|-|----------------|--------------------------|
| Sem Cache | 1759ms | 7-90ms |
| Com Cache | 130ms | 6-30ms |

**Lista todas avaliações de produtos**

| | **1ª Chamada** | **Chamadas Posteriores** |
|-|----------------|--------------------------|
| Sem Cache | 1723,5ms | 6-82ms |
| Com Cache | 2ms | 0ms |
| Com Cache - Tabela Avalições Inválida | 233ms | 21-48ms |

O uso da cache trouxe um ganho expressivo de performance em termos de tempo de resposta, especialmente nas primeiras chamadas das APIs.
Mesmo nos piores cenários (cache inválida no momento do get), o tempo de resposta foi significativamente menor do que sem cache.
Para as chamadas subsequentes, a aplicação demonstrou estar bem otimizada, não apresentando diferenças perceptíveis entre as execuções com ou sem cache.
