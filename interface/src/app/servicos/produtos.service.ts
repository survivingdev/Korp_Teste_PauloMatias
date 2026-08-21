import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  CriarProdutoRequisicao,
  Produto,
} from '../modelos/produto';

@Injectable({
  providedIn: 'root',
})
export class ProdutosService {
  private readonly http = inject(HttpClient);

  private readonly url =
    'http://127.0.0.1:5101/api/produtos';

  listar(): Observable<Produto[]> {
    return this.http.get<Produto[]>(this.url);
  }

  criar(
    requisicao: CriarProdutoRequisicao,
  ): Observable<Produto> {
    return this.http.post<Produto>(
      this.url,
      requisicao,
    );
  }
}
