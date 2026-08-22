export interface ItemNotaFiscal {
  produtoId: number;
  quantidade: number;
}

export interface NotaFiscal {
  numero: number;
  status: string;
  criadaEmUtc: string;
  itens: ItemNotaFiscal[];
}

export interface CriarNotaFiscalRequisicao {
  itens: ItemNotaFiscal[];
}
