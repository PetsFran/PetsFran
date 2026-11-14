import { Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { PrecoServico, PrecosService } from '../../core/services/precos.service';

type LinhaPreco = PrecoServico & { edicao: string; salvando: boolean; erro: string };

@Component({
  selector: 'app-gerenciar-precos',
  templateUrl: './gerenciar-precos.component.html',
  styleUrls: ['./gerenciar-precos.component.scss']
})
export class GerenciarPrecosComponent implements OnInit {
  linhas: LinhaPreco[] = [];
  carregando = true;
  erro = '';
  filtro = '';
  tipo: 'Base' | 'Adicional' = 'Base';
  porte = '';
  mensagem = '';

  constructor(private precos: PrecosService, public dialogRef: MatDialogRef<GerenciarPrecosComponent>) {}

  ngOnInit(): void { this.carregar(); }

  carregar(): void {
    this.carregando = true;
    this.erro = '';
    this.mensagem = '';
    this.precos.listar().subscribe({
      next: itens => {
        this.linhas = itens.map(item => ({ ...item, edicao: this.valorParaEdicao(item.valor), salvando: false, erro: '' }));
        this.carregando = false;
      },
      error: () => {
        this.erro = 'Não foi possível carregar os preços. Tente novamente.';
        this.carregando = false;
      }
    });
  }

  get portes(): string[] {
    return [...new Set(this.linhas.filter(item => item.tipo === this.tipo).map(item => item.porte))];
  }

  get visiveis(): LinhaPreco[] {
    const termo = this.filtro.trim().toLocaleLowerCase('pt-BR');
    return this.linhas.filter(item => item.tipo === this.tipo &&
      (!this.porte || item.porte === this.porte) &&
      (!termo || `${item.nome} ${item.raca} ${item.porte}`.toLocaleLowerCase('pt-BR').includes(termo)));
  }

  get personalizados(): number {
    return this.linhas.filter(item => item.personalizado).length;
  }

  identificar(_: number, item: LinhaPreco): string {
    return `${item.tipo}|${item.porte}|${item.raca}|${item.nome}`;
  }

  formatarValor(valor: number): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(valor);
  }

  nomePorte(porte: string): string {
    return ({ P: 'Pequeno', M: 'Médio', G: 'Grande' } as Record<string, string>)[porte] || porte;
  }

  podeSalvar(item: LinhaPreco): boolean {
    const valor = this.lerValor(item.edicao);
    return !item.salvando && (valor === null
      ? item.edicao.trim() !== this.valorParaEdicao(item.valor)
      : valor !== item.valor);
  }

  selecionarTipo(tipo: 'Base' | 'Adicional'): void {
    this.tipo = tipo;
    this.porte = '';
  }

  salvar(item: LinhaPreco): void {
    const valor = this.lerValor(item.edicao);
    if (valor === null) {
      item.erro = 'Informe um valor entre R$ 0,00 e R$ 99.999.999,99, com até duas casas decimais.';
      return;
    }
    if (valor === item.valor) return;
    item.salvando = true;
    item.erro = '';
    this.mensagem = '';
    this.precos.salvar(item, valor).subscribe({
      next: atualizado => {
        this.atualizar(item, atualizado);
        this.mensagem = `Preço de ${item.nome} salvo: ${this.formatarValor(atualizado.valor)}.`;
      },
      error: () => { item.erro = 'Não foi possível salvar o preço.'; item.salvando = false; }
    });
  }

  restaurar(item: LinhaPreco): void {
    item.salvando = true;
    item.erro = '';
    this.mensagem = '';
    this.precos.restaurar(item).subscribe({
      next: atualizado => {
        this.atualizar(item, atualizado);
        this.mensagem = `Preço padrão de ${item.nome} restaurado: ${this.formatarValor(atualizado.valor)}.`;
      },
      error: () => { item.erro = 'Não foi possível restaurar o preço.'; item.salvando = false; }
    });
  }

  private atualizar(item: LinhaPreco, atualizado: PrecoServico): void {
    item.valor = atualizado.valor;
    item.valorPadrao = atualizado.valorPadrao;
    item.personalizado = atualizado.personalizado;
    item.edicao = this.valorParaEdicao(atualizado.valor);
    item.salvando = false;
    item.erro = '';
  }

  private valorParaEdicao(valor: number): string {
    return valor.toFixed(2).replace('.', ',');
  }

  private lerValor(texto: string): number | null {
    const normalizado = texto.trim();
    if (!/^\d{1,8}(?:[,.]\d{1,2})?$/.test(normalizado)) return null;
    const valor = Number(normalizado.replace(',', '.'));
    return valor <= 99999999.99 ? valor : null;
  }
}
