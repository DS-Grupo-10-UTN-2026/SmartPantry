import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'app-pantry',
  templateUrl: './pantry.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LocalizationPipe],
})
export class PantryComponent { }
