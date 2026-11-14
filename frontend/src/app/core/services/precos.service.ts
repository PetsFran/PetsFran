import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PrecoServico {
  tipo: 'Base' | 'Adicional';
  porte: string;
  raca: string;
  nome: string;
  valor: number;
}

@Injectable({ providedIn: 'root' })
export class PrecosService {
  private readonly url = `${environment.apiUrl}/servicos/precos`;

  constructor(private http: HttpClient) {}

  listar(): Observable<PrecoServico[]> {
    return this.http.get<PrecoServico[]>(this.url);
  }

  salvar(item: PrecoServico, valor: number): Observable<PrecoServico> {
    return this.http.put<PrecoServico>(this.url, {
      tipo: item.tipo, porte: item.porte, raca: item.raca, nome: item.nome, valor
    });
  }

}
