import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { finalize } from 'rxjs';

import { Produto } from '../../modelos/produto';
import { ProdutosService } from '../../servicos/produtos.service';

@Component({
  selector: 'app-produtos',
  imports: [ReactiveFormsModule],
  templateUrl: './produtos.html',
  styleUrl: './produtos.scss',
})
export class Produtos implements OnInit {
  private readonly produtosService = inject(ProdutosService);
  private readonly formBuilder = inject(FormBuilder);

  readonly produtos = signal<Produto[]>([]);

  readonly carregando = signal(false);
  readonly salvando = signal(false);
  readonly falhaAoCarregar = signal(false);

  readonly mensagemErro = signal('');
  readonly mensagemSucesso = signal('');

  readonly formulario = this.formBuilder.nonNullable.group({
    codigo: [
      '',
      [
        Validators.required,
        Validators.maxLength(50),
      ],
    ],
    descricao: [
      '',
      [
        Validators.required,
        Validators.maxLength(200),
      ],
    ],
    saldo: [
      0,
      [
        Validators.required,
        Validators.min(0),
      ],
    ],
  });

  ngOnInit(): void {
    this.carregarProdutos();
  }

  carregarProdutos(): void {
    this.carregando.set(true);
    this.falhaAoCarregar.set(false);
    this.mensagemErro.set('');

    this.produtosService
      .listar()
      .pipe(
        finalize(() => {
          this.carregando.set(false);
        }),
      )
      .subscribe({
        next: (produtos) => {
          this.produtos.set(produtos);
          this.falhaAoCarregar.set(false);
        },
        error: (erro: HttpErrorResponse) => {
          this.falhaAoCarregar.set(true);

          this.mensagemErro.set(
            this.obterMensagemErro(erro),
          );
        },
      });
  }

  cadastrarProduto(): void {
    this.mensagemErro.set('');
    this.mensagemSucesso.set('');

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    this.salvando.set(true);

    const requisicao = this.formulario.getRawValue();

    this.produtosService
      .criar(requisicao)
      .pipe(
        finalize(() => {
          this.salvando.set(false);
        }),
      )
      .subscribe({
        next: (produto) => {
          this.mensagemSucesso.set(
            `Produto ${produto.codigo} cadastrado com sucesso.`,
          );

          this.formulario.reset({
            codigo: '',
            descricao: '',
            saldo: 0,
          });

          this.carregarProdutos();
        },
        error: (erro: HttpErrorResponse) => {
          this.mensagemErro.set(
            this.obterMensagemErro(erro),
          );
        },
      });
  }

  private obterMensagemErro(
    erro: HttpErrorResponse,
  ): string {
    if (erro.status === 409) {
      return (
        erro.error?.detail ??
        'Já existe um produto com esse código.'
      );
    }

    if (erro.status === 400) {
      return 'Confira os dados informados e tente novamente.';
    }

    if (erro.status === 0) {
      return 'Não foi possível conectar ao Serviço de Estoque.';
    }

    return (
      erro.error?.detail ??
      'Não foi possível concluir a operação.'
    );
  }
}
