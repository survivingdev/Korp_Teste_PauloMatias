export interface Produto {
  id: number;
  codigo: string;
  descricao: string;
  saldo: number;
}

export interface CriarProdutoRequisicao {
  codigo: string;
  descricao: string;
  saldo: number;
}
