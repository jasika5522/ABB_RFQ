import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-nav-card',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './nav-card.component.html',
  styleUrl: './nav-card.component.css'
})
export class NavCardComponent {
  @Input() title = '';
  @Input() description = '';
  @Input() route = '/';
  @Input() linkLabel = 'Open';
}