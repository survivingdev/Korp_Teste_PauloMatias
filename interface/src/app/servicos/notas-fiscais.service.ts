import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CriarNotaFiscalRequisicao,
  NotaFiscal,
} from '../modelos/nota-fiscal';

@Injectable({
  providedIn: 'root',
})
export class NotasFiscaisService {
  private readonly http = inject(HttpClient);

  private readonly url =
    'http://127.0.0.1:5102/api/notas-fiscais';

  listar(): Observable<NotaFiscal[]> {
    return this.http.get<NotaFiscal[]>(this.url);
  }

  criar(
    requisicao: CriarNotaFiscalRequisicao,
  ): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(
      this.url,
      requisicao,
    );
  }
}
