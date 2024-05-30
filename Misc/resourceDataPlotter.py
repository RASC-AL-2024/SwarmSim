import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
from scipy.signal import savgol_filter
import os

def ma(x, n):
  return np.convolve(x, np.ones(n), 'valid') / n

def j_to_kwh(x):
  return x / (3600 * 1000)

def w_to_kw(x):
  return x / 1000

def s_to_day(x):
  return x / 3600 / 24

def process(df):
  n = 100
  s = lambda x: ma(x, n)
  raw_generated = df['totalGenerated']
  raw_drained = raw_generated - df['battery']
  dt = np.diff(df['time'], prepend=-1)
  return {
    'time': s_to_day(df['time'][:-(n-1)] * 10), # technically incorrect but look better
    'battery': j_to_kwh(s(df['battery'])),
    'total_ore':  s(df['totalDirt']),
    'modules': ma(df['totalModules'], 30),
    'generated': w_to_kw(np.full_like(s(np.diff(raw_generated, prepend=raw_generated[0]) / dt), 1e4)), # fuck me
    'drained': w_to_kw(s(np.diff(raw_drained, prepend=raw_drained[0]) / dt))
  }

def trim(a, b, n):
  out_a, out_b = {}, {}
  for k, v in a.items():
    out_a[k] = v[:n]
    out_b[k] = b[k][:n]
  return out_a, out_b

growth = process(pd.read_csv(os.path.dirname(__file__) + '/../replicating.csv'))
baseline = process(pd.read_csv(os.path.dirname(__file__) + '/../baseline.csv'))
growth, baseline = trim(growth, baseline, 2000)

fig1, ax1 = plt.subplots(1, 1, figsize=(5, 5))
# ax1.plot(growth['time'], growth['battery'], label='Battery', color='blue')
# ax1.set_title('Battery Charge Over Time')
# ax1.set_xlabel('Time (s)')
# ax1.set_ylabel('Battery (kWh)')
# ax1.set_ylim(0, 300)
# ax1.grid(True)
# ax1.legend()

ax1.plot(growth['time'], growth['drained'], label='Consumption Rate')
ax1.plot(growth['time'], growth['generated'], label='Generation Rate')
ax1.set_title('Energy Flow Over Time')
ax1.set_xlabel('Time (Days)')
ax1.set_ylabel('kW')
ax1.grid(True)
ax1.legend()
fig1.savefig('energy.png', dpi=300)

# fig1.suptitle('Energy Simuluation Results')

plt.tight_layout()
plt.show()

fig2, ax1 = plt.subplots(1, 1, figsize=(5, 5))
# ax1.plot(growth['time'], growth['totalDirt'], label='Replicating')
# ax1.plot(growth['time'], baseline['totalDirt'], label='Baseline')
# ax1.set_xlabel('Time (s)')
# ax1.set_ylabel('Total Ore Collected (g)')
# ax1.set_title('Total Ore Collected Over Time')
# ax1.grid(True)
# ax1.legend()

ax1.plot(growth['time'], growth['modules'], label='Replicating')
ax1.plot(growth['time'], baseline['modules'], label='Baseline')
ax1.set_xlabel('Time (Days)')
ax1.set_ylabel('Total Modules Produced')
ax1.set_title('Total Modules Produced Over Time')
ax1.grid(True)
ax1.legend()
inset = ax1.inset_axes([0.65, 0.00, 0.35, 0.35], xlim=(0, 3), ylim=(0, 100), xticklabels=[], yticklabels=[])
inset.plot(growth['time'], growth['modules'], label='Replicating')
inset.plot(growth['time'], baseline['modules'], label='Baseline')
ax1.indicate_inset_zoom(inset, edgecolor='black')
fig2.savefig('modules.png', dpi=300)

plt.tight_layout()
plt.show()

# plt.plot(np.diff(baseline['modules']))
# plt.show()

# ax1.plot(*smooth(df['time'], df['battery']), label='Battery', color='blue')
# ax1.set_title('Battery Capacity Over Time')
# ax1.set_xlabel('Time')
# ax1.set_ylabel('Battery')
# ax1.grid(True)
# ax1.legend()
# 
# ax2.plot(*smooth(df['time'], df['totalDirt']), label='Total Ore', color='green')
# ax2.set_title('Total Ore Collected Over Time')
# ax2.set_xlabel('Time (s)')
# ax2.set_ylabel('Ore mass (g)')
# ax2.grid(True)
# ax2.legend()
# 
# generated = df['totalGenerated']
# drained = generated - df['battery']
# dt = np.diff(df['time'], prepend=-1)
# 
# ax4.plot(*smooth(df['time'], df['totalModules']), label='Total Modules', color='green')
# ax4.set_title('Total Modules Produced Over Time')
# ax4.set_xlabel('Time (s)')
# ax4.set_ylabel('Modules Produced')
# ax4.grid(True)
# ax4.legend()

