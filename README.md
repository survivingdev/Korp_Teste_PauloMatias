# Desafio Técnico Korp — Sistema de Emissão de Notas Fiscais

Implementação do desafio técnico da Korp para desenvolvimento de um sistema de emissão de notas fiscais.

## Status atual

Inicialização do projeto e definição da arquitetura.

## Objetivo

Desenvolver um sistema de emissão de notas fiscais com interface em Angular e backend baseado em microsserviços.

## Arquitetura planejada

- Interface em Angular
- Serviço de Estoque — ASP.NET Core
- Serviço de Faturamento — ASP.NET Core
- Persistência com PostgreSQL
- Comunicação HTTP entre os microsserviços
- Docker Compose para infraestrutura local

## Requisitos principais

A aplicação deverá permitir:

- cadastro de produtos com código, descrição e saldo;
- criação de notas fiscais com numeração sequencial;
- inclusão de múltiplos produtos e respectivas quantidades;
- utilização dos status `Aberta` e `Fechada`;
- processamento da nota com atualização do estoque;
- feedback visual de processamento e erros na interface;
- tratamento e recuperação de um cenário de falha entre microsserviços.

## Estrutura do repositório

A estrutura do projeto será documentada progressivamente conforme a implementação evoluir.

## Executando o projeto

As instruções completas de configuração e execução serão adicionadas progressivamente conforme os serviços e a interface forem implementados.
