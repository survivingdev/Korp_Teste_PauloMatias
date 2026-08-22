import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import {
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';

import { NotaFiscal } from '../../modelos/nota-fiscal';
import { Produto } from '../../modelos/produto';
import { NotasFiscaisService } from '../../servicos/notas-fiscais.service';
import { ProdutosService } from '../../servicos/produtos.service';

@Component({
  selector: 'app-notas-fiscais',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './notas-fiscais.html',
  styleUrl: './notas-fiscais.scss',
})
export class NotasFiscais implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly notasFiscaisService =
    inject(NotasFiscaisService);
  private readonly produtosService =
    inject(ProdutosService);

  readonly notas = signal<NotaFiscal[]>([]);
  readonly produtos = signal<Produto[]>([]);

  readonly carregando = signal(false);
  readonly salvando = signal(false);
  readonly notaProcessando = signal<number | null>(null);

  readonly mensagemErro = signal('');
  readonly mensagemSucesso = signal('');

  readonly formulario = this.formBuilder.group({
    itens: this.formBuilder.array([
      this.criarGrupoItem(),
    ]),
  });

  ngOnInit(): void {
    this.carregarDados();
  }

  get itens(): FormArray {
    return this.formulario.controls.itens;
  }

  carregarDados(): void {
    this.carregando.set(true);
    this.mensagemErro.set('');

    forkJoin({
      notas: this.notasFiscaisService.listar(),
      produtos: this.produtosService.listar(),
    })
      .pipe(
        finalize(() => {
          this.carregando.set(false);
        }),
      )
      .subscribe({
        next: ({ notas, produtos }) => {
          this.notas.set(
            [...notas].sort(
              (notaA, notaB) => notaB.numero - notaA.numero,
            ),
          );

          this.produtos.set(produtos);
        },
        error: (erro: HttpErrorResponse) => {
          this.mensagemErro.set(
            this.obterMensagemErro(erro),
          );
        },
      });
  }

  adicionarItem(): void {
    this.itens.push(this.criarGrupoItem());
  }

  removerItem(indice: number): void {
    if (this.itens.length === 1) {
      return;
    }

    this.itens.removeAt(indice);
  }

  criarNota(): void {
    this.mensagemErro.set('');
    this.mensagemSucesso.set('');

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const itens = this.itens.getRawValue().map(
      (item) => ({
        produtoId: Number(item.produtoId),
        quantidade: Number(item.quantidade),
      }),
    );

    const produtosSelecionados = itens.map(
      (item) => item.produtoId,
    );

    if (
      new Set(produtosSelecionados).size !==
      produtosSelecionados.length
    ) {
      this.mensagemErro.set(
        'Um produto não pode aparecer mais de uma vez na mesma nota.',
      );

      return;
    }

    this.salvando.set(true);

    this.notasFiscaisService
      .criar({ itens })
      .pipe(
        finalize(() => {
          this.salvando.set(false);
        }),
      )
      .subscribe({
        next: (nota) => {
          this.mensagemSucesso.set(
            `Nota fiscal nº ${nota.numero} criada com sucesso.`,
          );

          this.formulario.setControl(
            'itens',
            this.formBuilder.array([
              this.criarGrupoItem(),
            ]),
          );

          this.carregarDados();
        },
        error: (erro: HttpErrorResponse) => {
          this.mensagemErro.set(
            this.obterMensagemErro(erro),
          );
        },
      });
  }

  processarNota(nota: NotaFiscal): void {
    if (
      nota.status !== 'Aberta' ||
      this.notaProcessando() !== null
    ) {
      return;
    }

    this.mensagemErro.set('');
    this.mensagemSucesso.set('');
    this.notaProcessando.set(nota.numero);

    this.notasFiscaisService
      .processar(nota.numero)
      .pipe(
        finalize(() => {
          this.notaProcessando.set(null);
        }),
      )
      .subscribe({
        next: (notaProcessada) => {
          this.mensagemSucesso.set(
            `Nota fiscal nº ${notaProcessada.numero} processada com sucesso.`,
          );

          this.carregarDados();
        },
        error: (erro: HttpErrorResponse) => {
          this.mensagemErro.set(
            this.obterMensagemProcessamento(erro),
          );
        },
      });
  }

  descricaoProduto(produtoId: number): string {
    const produto = this.produtos().find(
      (item) => item.id === produtoId,
    );

    return produto
      ? `${produto.codigo} — ${produto.descricao}`
      : `Produto #${produtoId}`;
  }

  private criarGrupoItem(): FormGroup {
    return this.formBuilder.group({
      produtoId: [
        null,
        Validators.required,
      ],
      quantidade: [
        1,
        [
          Validators.required,
          Validators.min(1),
        ],
      ],
    });
  }

  private obterMensagemProcessamento(
    erro: HttpErrorResponse,
  ): string {
    if (erro.status === 409) {
      return (
        erro.error?.detail ??
        'A nota fiscal não pôde ser processada.'
      );
    }

    if (erro.status === 503) {
      return (
        erro.error?.detail ??
        'O Serviço de Estoque está indisponível. Tente novamente.'
      );
    }

    if (erro.status === 0) {
      return 'Não foi possível conectar ao Serviço de Faturamento.';
    }

    return (
      erro.error?.detail ??
      'Não foi possível processar a nota fiscal.'
    );
  }

  private obterMensagemErro(
    erro: HttpErrorResponse,
  ): string {
    if (erro.status === 400) {
      return 'Confira os itens da nota fiscal e tente novamente.';
    }

    if (erro.status === 0) {
      return 'Não foi possível conectar aos serviços necessários.';
    }

    return (
      erro.error?.detail ??
      'Não foi possível concluir a operação.'
    );
  }
}
